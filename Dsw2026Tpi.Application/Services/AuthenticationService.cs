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
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IPatientService _patientService;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        ISignInService signInManager,
        JwtService jwtService,
        ILogger<AuthenticationService> logger,
        IPatientService patientService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _logger = logger;
        _patientService = patientService;
    }

    public async Task<LoginAdminModel.Response> LoginAdmin(LoginAdminModel.Request request)
    {
        ValidateEmail(request.Email);

        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null)
        {
            //No damos informacíon adicional sobre si existe o no el email
            _logger.LogWarning("Intento de login administrador fallido para {Email}", request.Email);
            throw new AuthenticationException().WithDetail("User", "Invalid credentials");
        }

        EnsureUserIsActive(user);

        var passwordValid = await _signInManager.CheckPassword(user, request.Password);

        if (!passwordValid)
        {
            _logger.LogWarning("Intento de login administrador fallido para {Email}", request.Email);
            throw new AuthenticationException();
        }

        await EnsureUserHasRole(user, Roles.Administrator);

        var token = _jwtService.GenerateToken(user.Id, user.UserName!, Roles.Administrator);

        _logger.LogInformation("Login administrador exitoso para {Email}", request.Email);

        return new LoginAdminModel.Response(token, Roles.Administrator);
    }

    public async Task<LoginPatientModel.Response> LoginPatient(LoginPatientModel.Request request)
    {
        ValidateEmail(request.Email);
        ValidateDni(request.Dni);

        var dni = request.Dni.ToString();
        var patient = await _patientService.GetByDni(dni);

        ApplicationUser? user;

        if (patient is null)
        {
            var result = await GetOrCreatePatientUser(request.Email);

            user = result.User;

            await EnsureUserDoesNotHaveAnotherPatient(user);

            try
            {
                await _patientService.CreatePatient(user.Id, dni);
                _logger.LogInformation("Paciente creado correctamente. DNI: {Dni}, UserId: {UserId}", dni, user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo crear el paciente {Dni} para el usuario {UserId}", request.Dni, user.Id);

                if (result.Created) await RollbackUserCreation(user);

                throw;
            }
        }
        else //Sí existe paciente
        {
            //Buscamos el usuario asociado al patient
            user = await GetPatientUser(patient);
            //Valido el mail y rol del usuario obtenido
            await ValidatePatientUser(user, request.Email);
        }

        var token = _jwtService.GenerateToken(user.Id, user.UserName!, Roles.Patient);

        _logger.LogInformation("Login paciente exitoso para {Email}", request.Email);

        return new LoginPatientModel.Response(token, Roles.Patient);
    }

    public async Task<RegisterModel.Response> Register(RegisterModel.Request request)
    {
        ValidateEmail(request.Email);

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Deleted = false
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            _logger.LogWarning("No se pudo registrar el usuario {Email}", request.Email);
            throw new ConflictException(nameof(ErrorCodes.REGISTER_USER_CONFLICT), ErrorCodes.REGISTER_USER_CONFLICT).WithDetail(result.Errors.Select(e => (e.Code, e.Description)));
        }

        try
        {
            await AddRole(user, Roles.Administrator);
        }
        catch
        {
            await RollbackUserCreation(user);
            throw;
        }

        _logger.LogInformation("Usuario registrado: {Email}", request.Email);

        return new RegisterModel.Response(request.Email);
    }

    private static void ValidateEmail(string email)
    {
        if (!string.IsNullOrWhiteSpace(email) && email.IsEmailValid()) return;

        throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR).WithDetail("email", "Email is invalid");
    }

    private static void ValidateDni(long dni)
    {
        if (dni >= 1_000_000 && dni <= 99_999_999) return;

        throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR).WithDetail("dni", "DNI is invalid");
    }

    private void EnsureUserIsActive(ApplicationUser user)
    {
        if (user.Deleted)
        {
            _logger.LogWarning("El usuario {UserId} está eliminado", user.Id);
            throw new AuthenticationException().WithDetail("Authentication", "User is inactive");
        }
    }

    private async Task EnsureUserHasRole(ApplicationUser user, string role)
    {
        var hasRole = await _userManager.IsInRoleAsync(user, role);

        if (!hasRole)
        {
            _logger.LogWarning("El usuario {UserId} intentó autenticarse " + "sin el rol requerido {Role}", user.Id, role);
            throw new AuthenticationException().WithDetail("Authentication", "Invalid Credentials");
        }
    }

    private async Task AddRole(ApplicationUser user, string role)
    {
        var result = await _userManager.AddToRoleAsync(user, role);

        if (result.Succeeded) return;

        _logger.LogError("No se pudo asignar el rol {Role} al usuario {UserId}.", role, user.Id);
        throw new ConflictException(nameof(ErrorCodes.REGISTER_USER_CONFLICT), ErrorCodes.REGISTER_USER_CONFLICT).WithDetail(result.Errors.Select(e => (e.Code, e.Description)));
    }

    private async Task RollbackUserCreation(ApplicationUser user)
    {
        var result = await _userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            _logger.LogCritical("No se pudo revertir la creación del usuario {UserId}.", user.Id);
            return;
        }

        _logger.LogInformation("Se revirtió correctamente la creación del usuario {UserId}.", user.Id);
    }

    private async Task EnsureUserDoesNotHaveAnotherPatient(ApplicationUser user)
    {
        //Compruebo que el usuario no tenga asignado otro paciente, no debo crear un segundo Patient para el mismo ApplicationUser
        var existingPatient = await _patientService.GetByUserId(user.Id);

        if (existingPatient is null) return;

        // El usuario ya tiene un paciente asociado, pero el DNI enviado no coincide con ese paciente.
        // Por lo tanto, no permitimos crear otro paciente.
        _logger.LogWarning("El usuario {UserId} ya posee un paciente asociado con DNI {Dni}.", user.Id, existingPatient.Dni);
        throw new AuthenticationException().WithDetail("Patient", "User Already Has Patient");
    }

    private async Task<(ApplicationUser User, bool Created)> GetOrCreatePatientUser(string email)
    {
        //Compruebo que no haya usuario con el email
        var user = await _userManager.FindByEmailAsync(email);

        if (user is not null)
        {
            // Un usuario marcado como eliminado no puede autenticarse.
            if (user.Deleted)
            {
                _logger.LogWarning("El usuario eliminado {Email} intentó iniciar sesión como paciente.", email);
                throw new AuthenticationException().WithDetail("User", "User is Inactive");
            }

            //compruebo que el usuario tenga el rol de paciente
            await EnsureUserHasRole(user, Roles.Patient);
            return (user, false);
        }

        //Sí el usuario es null lo creo...
        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Deleted = false
        };

        var result = await _userManager.CreateAsync(user);

        if (!result.Succeeded)
        {
            _logger.LogWarning("No se pudo crear automáticamente el usuario paciente {Email}.", email);
            throw new ConflictException(nameof(ErrorCodes.REGISTER_USER_CONFLICT), ErrorCodes.REGISTER_USER_CONFLICT).WithDetail(result.Errors.Select(e => (e.Code, e.Description)));
        }

        try
        {
            await AddRole(user, Roles.Patient);
        }
        catch
        {
            await RollbackUserCreation(user);
            throw;
        }

        return (user, true);
    }

    private async Task<ApplicationUser> GetPatientUser(Patient patient)
    {
        var user = await _userManager.FindByIdAsync(patient.UserId.ToString());

        if (user is null)
        {
            _logger.LogError("El paciente {PatientId} tiene asociado el UserId {UserId}, " + "pero dicho usuario no existe en Identity.", patient.Id, patient.UserId);
            throw new AuthenticationException().WithDetail("Patient", "Associated User Not Found");
        }

        return user;
    }

    private async Task ValidatePatientUser(ApplicationUser user, string email)
    {
        if (user.Deleted)
        {
            _logger.LogWarning("El usuario {Email} está eliminado.", email);
            throw new AuthenticationException().WithDetail("User", "Useris inactive");
        }

        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("El email {Email} no coincide con el email " + "del usuario asociado al paciente.", email);
            throw new AuthenticationException().WithDetail("Credentials", "Invalid Credentials");
        }

        await EnsureUserHasRole(user, Roles.Patient);
    }
}

/*
En Paciente se presenta un problema, y es que al crear un usuario y luego un paciente, estoy accediendo a DbContext distintos, pudiendo ocurrir que funcione la creación del usuario pero no la del patient, y debo compensarlo o solucionarlo 
 
*/
