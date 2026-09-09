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
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IPatientService _patientService;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        ISignInService signInManager,
        RoleManager<ApplicationRole> roleManager,
        JwtService jwtService,
        ILogger<AuthenticationService> logger,
        IPatientService patientService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _jwtService = jwtService;
        _logger = logger;
        _patientService = patientService;
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

        var token = _jwtService.GenerateToken(user.Id, user.UserName!, Roles.Administrator);

        _logger.LogInformation("Login administrador exitoso para {Email}", request.Email);

        return new LoginAdminModel.Response(
            token,
            Roles.Administrator
        );
    }

    public async Task<LoginPatientModel.Response> LoginPatient(LoginPatientModel.Request request)
    {
        ApplicationUser? user;
        bool userCreated = false;

        if (string.IsNullOrWhiteSpace(request.Email) ||
            !request.Email.IsEmailValid())
        {
            throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR).WithDetail("email", "invalid_email");
        }

        if (request.Dni < 1_000_000 || request.Dni > 99_999_999)
        {
            throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR).WithDetail("dni", "invalid_dni");
        }

        var dni = request.Dni.ToString();

        var patient = await _patientService.GetByDni(dni);

        if (patient == null) //Sí el paciente no existe, me crea usuario+paciente
        {
            //Compruebo que no haya usuario con el email
            user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null) //Si no existe un usuario con ese mail, lo creo
            {
                user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    //se esta creando sin contraseña
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                var result = await _userManager.CreateAsync(user);

                if (!result.Succeeded)
                {
                    _logger.LogWarning("No se pudo crear automáticamente el usuario paciente {Email}", request.Email);
                    throw new ConflictException(nameof(ErrorCodes.REGISTER_USER_CONFLICT), ErrorCodes.REGISTER_USER_CONFLICT)
                        .WithDetail(result.Errors.Select(e => (e.Code, e.Description)));
                }

                userCreated = true;//Aquí ya sabemos que el ApplicationUser se creo

                var roleResult = await _userManager.AddToRoleAsync(user, Roles.Patient);

                if (!roleResult.Succeeded)
                {
                    _logger.LogError("No se pudo asignar el rol paciente al usuario {Email}", request.Email);

                    //Si no puedo cargar el rol, debo eliminar el usuario creado

                    var deleteResult = await _userManager.DeleteAsync(user);

                    if (!deleteResult.Succeeded) _logger.LogCritical(
                            "No se pudo revertir la creación del usuario " +
                            "{UserId} después de fallar la asignación del rol",
                            user.Id);

                    throw new ConflictException(nameof(ErrorCodes.REGISTER_USER_CONFLICT), ErrorCodes.REGISTER_USER_CONFLICT)
                        .WithDetail(roleResult.Errors.Select(e => (e.Code, e.Description)));
                }
            }
            else //En caso de que existe un usuario con ese mail, compruebo que tenga rol de Paciente
            {
                // Un usuario marcado como eliminado no puede autenticarse.
                if (user.Deleted)
                {
                    _logger.LogWarning("El usuario eliminado {Email} intentó iniciar sesión como paciente", request.Email);
                    throw new AuthenticationException().WithDetail("User", "El usuario se encuentra inactivo.");
                }

                var isPatient = await _userManager.IsInRoleAsync(user, Roles.Patient);

                if (!isPatient)
                {
                    _logger.LogWarning("El usuario {Email} intentó acceder al login de paciente sin rol PACIENTE", request.Email);
                    throw new AuthenticationException();
                }

                //Compruebo que el usuario no tenga asignado otro paciente, no debo crear un segundo Patient para el mismo ApplicationUser
                var existingPatient = await _patientService.GetByUserId(user.Id);

                if (existingPatient != null)
                {
                    // El usuario ya tiene un paciente asociado, pero el DNI enviado no coincide con ese paciente.
                    // Por lo tanto, no permitimos crear otro paciente.
                    _logger.LogWarning("El usuario {Email} ya posee un paciente asociado " + "con otro DNI", request.Email);
                    throw new AuthenticationException().WithDetail("Patient", "Su usuario ya posee otro Patient");
                }
            }

            try
            {
                await _patientService.CreatePatient(user.Id, Convert.ToString(request.Dni));
                _logger.LogInformation("Entidad paciente registrada: {Dni}", request.Dni);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo crear el paciente {Dni} para el usuario {UserId}", request.Dni, user.Id);

                if (userCreated)
                {
                    var deleteResult = await _userManager.DeleteAsync(user);

                    if (!deleteResult.Succeeded)
                    {
                        _logger.LogCritical("No se pudo revertir la creación del usuario " + "{UserId} después de fallar la creación del paciente", user.Id);
                    }
                    else
                    {
                        _logger.LogInformation("Se revirtió correctamente la creación del usuario " + "{UserId}", user.Id);
                    }
                }

                throw;
            }

        }
        else //Sí existe paciente
        {
            //Buscamos el usuario asociado al patient
            user = await _userManager.FindByIdAsync(patient.UserId.ToString());

            if (user == null)
            {
                _logger.LogError("El paciente {PatientId} tiene asociado un UserId " + "{UserId} que no existe en Identity", patient.Id, patient.UserId);
                throw new AuthenticationException();
            }

            if (user.Deleted)
            {
                _logger.LogWarning("El usuario {Email} está eliminado", request.Email);
                throw new AuthenticationException();
            }

            if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("El Email no coincide con el DNI proporcionado: {Email}", request.Email);
                throw new AuthenticationException();
            }
            var isPatient = await _userManager.IsInRoleAsync(user, Roles.Patient);

            if (!isPatient)
            {
                _logger.LogWarning("El usuario {Email} no posee el rol Paciente", request.Email);
                throw new AuthenticationException();
            }
        }

        var token = _jwtService.GenerateToken(user.Id, user.UserName!, Roles.Patient);

        _logger.LogInformation("Login paciente exitoso para {Email}", request.Email);

        return new LoginPatientModel.Response(token, Roles.Patient);
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

/*
En Paciente se presenta un problema, y es que al crear un usuario y luego un paciente, estoy accediendo a DbContext distintos, pudiendo ocurrir que funcione la creación del usuario pero no la del patient, y debo compensarlo o solucionarlo 
 
*/
