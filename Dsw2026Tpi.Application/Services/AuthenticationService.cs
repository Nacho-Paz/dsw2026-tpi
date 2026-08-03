using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Dsw2026Tpi.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISignInService _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(UserManager<ApplicationUser> userManager,
        ISignInService signInManager,
        RoleManager<IdentityRole> roleManager,
        JwtService jwtService,
        ILogger<AuthenticationService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<LoginAdminModel.Response> LoginAdmin(LoginAdminModel.Request request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            !request.Email.IsEmailValid())
        {
            //TODO: Imprimir esto con ErrorCodes y sumar log
            throw new ValidationException("Email invalido", "Error");
            //throw new ValidationException(ErrorCodes.REGISTER_USER_INVALID,nameof(ErrorCodes.REGISTER_USER_INVALID));
        }
        if (string.IsNullOrWhiteSpace(request.Password) ||
            request.Password.Length < 8)
        {
            //TODO: Imprimir esto con ErrorCodes y sumar log
            throw new ValidationException("Password invalido", "Error");
        }

        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null)
        {
            _logger.LogWarning("Intento de login administrador fallido para {Email}", request.Email);
            //throw new AuthenticationException();
            throw new ValidationException("Ese email no se encuentra registrado", "Error");
        }

        var passwordValid = await _signInManager.CheckPassword(user, request.Password);

        if (!passwordValid)
        {
            _logger.LogError("Intento de login fallido para: {Email}", request.Email);
            //throw new AuthenticationException();
            throw new ValidationException("Password invalido", "Error");
        }

        var role = await _userManager.GetRolesAsync(user);

        if (!role.Contains(Roles.Administrator))
        {
            _logger.LogWarning("Usuario {Email} intentó acceder al login de administrador", request.Email);

            //throw new AuthenticationException();
            throw new ValidationException("Su email no cuenta con el rol ADMINISTADOR", "Error");
        }

        var token = _jwtService.GenerateToken(user.UserName!, Roles.Administrator);

        return new LoginAdminModel.Response(
            token,
            Roles.Administrator
        );
    }

    public async Task<LoginPatientModel.Response> LoginPatient(LoginPatientModel.Request request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.IsEmailValid())
        {
            throw new ValidationException();
        }

        if (request.Dni < 1_000_000 || request.Dni > 99_999_999)
        {
            throw new ValidationException();
        }

        // Buscar paciente por email
        var user = await _userManager.FindByEmailAsync(request.Email);

        // Si no existe, crearlo automáticamente
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                Dni = Convert.ToString(request.Dni),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user);

            if (!createResult.Succeeded)
            {
                throw new ConflictException(
                    nameof(ErrorCodes.REGISTER_USER_CONFLICT),
                    ErrorCodes.REGISTER_USER_CONFLICT)
                    .WithDetail(
                        createResult.Errors.Select(
                            e => (e.Code, e.Description)));
            }

            var roleResult = await _userManager.AddToRoleAsync(
                user,
                Roles.Patient);

            if (!roleResult.Succeeded)
            {
                throw new ConflictException(
                    nameof(ErrorCodes.REGISTER_USER_CONFLICT),
                    ErrorCodes.REGISTER_USER_CONFLICT)
                    .WithDetail(
                        roleResult.Errors.Select(
                            e => (e.Code, e.Description)));
            }

            _logger.LogInformation(
                "Paciente creado automáticamente: {Email}",
                request.Email);
        }
        else
        {
            // Si existe, verificar que el DNI coincida
            if (user.Dni != Convert.ToString(request.Dni))
            {
                _logger.LogWarning(
                    "Intento de acceso con DNI incorrecto para {Email}",
                    request.Email);

                throw new AuthenticationException();
            }

            // Verificar que tenga rol paciente
            var isPatient = await _userManager.IsInRoleAsync(
                user,
                Roles.Patient);

            if (!isPatient)
            {
                throw new AuthenticationException();
            }
        }

        // Generar JWT
        var token = _jwtService.GenerateToken(
            user.UserName!,
            Roles.Patient);

        _logger.LogInformation(
            "Login paciente exitoso para {Email}",
            request.Email);

        return new LoginPatientModel.Response(
            token,
            Roles.Patient);
    }

    public async Task<RegisterModel.Response> Register(RegisterModel.Request request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.IsEmailValid())
        {
            throw new ValidationException(ErrorCodes.REGISTER_USER_INVALID, nameof(ErrorCodes.REGISTER_USER_INVALID));
        } //TODO: Terminar de depurar y seguir este método para ver como se forma el error

        if (string.IsNullOrWhiteSpace(request.Password) ||
            request.Password.Length < 8)
        {
            throw new ValidationException(
                ErrorCodes.REGISTER_USER_INVALID,
                nameof(ErrorCodes.REGISTER_USER_INVALID));
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded) throw new ConflictException(nameof(ErrorCodes.REGISTER_USER_CONFLICT),
            ErrorCodes.REGISTER_USER_CONFLICT)
                .WithDetail(result.Errors.Select(e => (e.Code, e.Description)));

        _ = await _userManager.AddToRoleAsync(user, Roles.Administrator);

        _logger.LogInformation("Usuario registrado: {Email}", request.Email);

        return new RegisterModel.Response(request.Email);
    }
}
