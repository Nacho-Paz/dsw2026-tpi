using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Data.Identity;
using Dsw2026Tpi.Domain.Entities;
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
    private readonly IPatientService _petientService;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        ISignInService signInManager,
        RoleManager<IdentityRole> roleManager,
        JwtService jwtService,
        ILogger<AuthenticationService> logger,
        IPatientService patientService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _jwtService = jwtService;
        _logger = logger;
        _petientService = patientService;
    }

    public async Task<LoginAdminModel.Response> LoginAdmin(LoginAdminModel.Request request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.IsEmailValid())
        {
            throw new ValidationException(nameof(ErrorCodes.REGISTER_USER_INVALID), ErrorCodes.REGISTER_USER_INVALID)
                .WithDetail("Email", "Invalid_Email");
        }

        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null)
        {
            _logger.LogWarning("Intento de login administrador fallido para {Email}", request.Email);
            throw new AuthenticationException();
            //.WithDetail("User","User_Not_Found");
        }

        var passwordValid = await _signInManager.CheckPassword(user, request.Password);

        if (!passwordValid)
        {
            _logger.LogError("Intento de login fallido para: {Email}", request.Email);
            throw new AuthenticationException();
        }
        //var isAdministrator = await _userManager.IsInRoleAsync(user,Roles.Administrator); TODO: Probar luego con esto
        var role = await _userManager.GetRolesAsync(user);

        if (!role.Contains(Roles.Administrator))
        {
            _logger.LogWarning("Usuario {Email} intentó acceder al login de administrador", request.Email);
            throw new AuthenticationException();
            //.WithDetail("Su email no cuenta con el rol ADMINISTADOR", "Error");
        }

        var token = _jwtService.GenerateToken(user.UserName!, Roles.Administrator);

        _logger.LogInformation("Login administrador exitoso para {Email}", request.Email);

        return new LoginAdminModel.Response(
            token,
            Roles.Administrator
        );
    }

    public async Task<LoginPatientModel.Response> LoginPatient(LoginPatientModel.Request request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            !request.Email.IsEmailValid())
        {
            throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR)
                .WithDetail("email", "invalid_email");
        }

        if (request.Dni < 1_000_000 || request.Dni > 99_999_999)
        {
            throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR)
                .WithDetail("dni", "invalid_dni");
        }

        var patient = await _petientService.GetByDni(Convert.ToString(request.Dni));

        if (patient == null)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                var result = await _userManager.CreateAsync(user);
                
                if (!result.Succeeded)
                {
                    _logger.LogWarning(
                    "No se pudo crear automáticamente el usuario paciente {Email}",
                    request.Email);

                    throw new ConflictException(nameof(ErrorCodes.REGISTER_USER_CONFLICT),ErrorCodes.REGISTER_USER_CONFLICT)
                        .WithDetail(result.Errors.Select(e => (e.Code, e.Description)));
                }

                var roleResult = await _userManager.AddToRoleAsync(user, Roles.Patient);

                if (!roleResult.Succeeded)
                {
                    _logger.LogError("No se pudo asignar el rol paciente al usuario {Email}",request.Email);

                    throw new ConflictException(nameof(ErrorCodes.REGISTER_USER_CONFLICT), ErrorCodes.REGISTER_USER_CONFLICT)
                        .WithDetail(roleResult.Errors.Select(e => (e.Code, e.Description)));
                }
            }
            else
            {
                var isPatient = await _userManager.IsInRoleAsync(user,Roles.Patient);

                if (!isPatient)
                {
                    _logger.LogWarning("El usuario {Email} intentó acceder al login de paciente sin rol PACIENTE",request.Email);

                    throw new AuthenticationException();
                }
            }

            var patientToService = new Patient(Guid.Parse(user.Id), Convert.ToString(request.Dni));

            _logger.LogInformation("Entidad paciente registrada: {Dni}",request.Dni);
        }
        else
        {
            var user = await _userManager.FindByIdAsync(patient.UserId.ToString());

            if (user == null ||!string.Equals(user.Email,request.Email,StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("El Email no coincide con el DNI proporcionado: {Email}",request.Email);

                throw new AuthenticationException();
            }
            var isPatient = await _userManager.IsInRoleAsync(user,Roles.Patient);

            if (!isPatient)
            {
                _logger.LogWarning("El usuario {Email} no posee el rol Paciente",request.Email);

                throw new AuthenticationException();
            }
        }

        var token = _jwtService.GenerateToken(Convert.ToString(request.Dni),Roles.Patient);

        _logger.LogInformation("Login paciente exitoso para {Email}",request.Email);

        return new LoginPatientModel.Response(token,Roles.Patient);
    }

    public async Task<RegisterModel.Response> Register(RegisterModel.Request request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.IsEmailValid())
        {
            throw new ValidationException(nameof(ErrorCodes.REGISTER_USER_INVALID), ErrorCodes.REGISTER_USER_INVALID)
                .WithDetail("email", "invalid_email");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            _logger.LogWarning(
                "No se pudo registrar el usuario {Email}",
                request.Email);

            throw new ConflictException(
                nameof(ErrorCodes.REGISTER_USER_CONFLICT),
                ErrorCodes.REGISTER_USER_CONFLICT)
                .WithDetail(result.Errors.Select(e => (e.Code, e.Description)));
        }


        var roleResult = await _userManager.AddToRoleAsync(user, Roles.Administrator);

        //TODO: Validación de role de forma preventiva 
        if (!roleResult.Succeeded)
        {
            _logger.LogError(
                "No se pudo asignar el rol administrador al usuario {Email}",
                request.Email);

            throw new ConflictException(
                nameof(ErrorCodes.REGISTER_USER_CONFLICT),
                ErrorCodes.REGISTER_USER_CONFLICT)
                .WithDetail(roleResult.Errors.Select(e => (e.Code, e.Description)));
        }

        _logger.LogInformation("Usuario registrado: {Email}", request.Email);

        return new RegisterModel.Response(request.Email);
    }
}
