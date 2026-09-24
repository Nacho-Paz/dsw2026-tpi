using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace Dsw2026Tpi.Application.Services;

public class DoctorService : IDoctorService
{
    private readonly IPersistence _persistence;
    private readonly ILogger<DoctorService> _logger;
    public DoctorService(IPersistence persistence, ILogger<DoctorService> logger)
    {
        _persistence = persistence;
        _logger = logger;
    }

    public async Task<Pagination<DoctorModel.Response>> GetAll(int pageSize, int pageIndex, string? name = null)

    {
        ValidateDoctorName(name);

        var doctors = await _persistence.Paginate<Doctor, string>(
        pageSize,
        pageIndex,
        d => d.IsActive && (string.IsNullOrWhiteSpace(name) || d.Name.Contains(name)),
        x => x.Name,
        nameof(Doctor.Specialty));

        return doctors.Map(d => MapToResponse(d));
    }

    public async Task<List<DoctorModel.AvailabilityResponse>> GetDoctorAvailabilities(Guid doctorId)
    {
        ValidateId(doctorId, "Doctor");
        var doctor = await _persistence.First<Doctor>(d => d.Id == doctorId && d.IsActive);

        if (doctor is null)
        {
            _logger.LogWarning("No se encontró el médico activo {DoctorId} al consultar disponibilidades.", doctorId);
            throw new EntityNotFoundException("Doctor").WithDetail("doctorId", "Doctor not found");
        }

        var now = DateTime.UtcNow;
        var rules = await _persistence.GetFiltered<AvailabilityRule>(
            r => r.DoctorId == doctorId && r.Month == now.Month && r.Year == now.Year && !r.Deleted,
            "Slots");

        if (rules == null || !rules.Any()) return [];

        return rules.Select(r => new DoctorModel.AvailabilityResponse(
            r.Id,
            r.DayOfWeek.ToString(),
            r.StartTime.ToString(@"hh\:mm"),
            r.EndTime.ToString(@"hh\:mm"))).ToList();

    }

    public async Task<DoctorModel.Response?> CreateDoctor(DoctorModel.Request model)
    {
        _logger.LogInformation("Iniciando la creación de un nuevo médico: {Name}", model.Name);
        ValidateDoctorName(model.Name);

        // 1. Buscamos la especialidad de forma segura con GetFiltered (evita que explote si no existe)
        var speciality = await GetActiveSpecialty(model.specialtyId);
        await EnsureLicenseNumberIsAvailable(model.LicenseNumber);

        var newDoctor = new Doctor(model.Name, model.LicenseNumber, speciality);
        await _persistence.Add(newDoctor);
        _logger.LogInformation("Médico creado exitosamente con ID {DoctorId}", newDoctor.Id);
        return MapToResponse(newDoctor);
    }

    public async Task<DoctorModel.Response?> UpdateDoctor(Guid id, DoctorModel.Request model)
    {
        _logger.LogInformation("Iniciando la actualización del médico con ID: {DoctorId}", id);
        ValidateId(id, "Doctor");
        ValidateDoctorName(model.Name);

        var doctor = await _persistence.First<Doctor>(d => d.Id == id && d.IsActive, nameof(Doctor.Specialty));

        if (doctor is null)
        {
            _logger.LogWarning("No se encontró el médico activo {DoctorId} para actualizar.", id);
            throw new EntityNotFoundException("Doctor").WithDetail("doctorId", "Doctor not found");
        }

        var specialty = await GetActiveSpecialty(model.specialtyId);
        await EnsureLicenseNumberIsAvailable(model.LicenseNumber, id);

        doctor.UpdateData(
            model.Name,
            model.LicenseNumber,
            specialty);
        await _persistence.Update(doctor);

        _logger.LogInformation("Médico con ID {DoctorId} actualizado exitosamente.", id);
        return MapToResponse(doctor);
    }

    public async Task<bool> DeleteDoctor(Guid id)
    {
        _logger.LogInformation("Iniciando la desactivación del médico con ID: {DoctorId}", id);

        ValidateId(id, "Doctor");
        var doctor = await _persistence.First<Doctor>(d => d.Id == id && d.IsActive);

        if (doctor is null)
        {
            _logger.LogWarning("No se encontró el médico activo {DoctorId} para desactivar.", id);
            throw new EntityNotFoundException("Doctor").WithDetail("doctorId", "Doctor not found");
        }

        doctor.Deactivate();
        await _persistence.Update(doctor);
        _logger.LogInformation("Médico con ID {DoctorId} desactivado exitosamente.", id);
        return true;
    }

    private void ValidateDoctorName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length < 3 || name.Length > 100)
        {
            _logger.LogWarning("Nombre de médico inválido: {Name}", name);
            throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR).WithDetail("name", "Doctor name must contain between 3 and 100 characters.");
        }
    }

    private static void ValidateId(Guid id, string entityName)
    {
        if (id != Guid.Empty) return;
        throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR).WithDetail("id", $"{entityName} id is required.");
    }

    private static DoctorModel.Response MapToResponse(Doctor doctor)
    {
        return new DoctorModel.Response(
            doctor.Id,
            doctor.Name,
            doctor.LicenseNumber,
            new DoctorModel.SpecialtyDto(doctor.Specialty?.Id, doctor.Specialty?.Name));
    }

    private async Task<Specialty> GetActiveSpecialty(Guid specialtyId)
    {
        ValidateId(specialtyId, "Specialty");

        var specialty = await _persistence.First<Specialty>(s => s.Id == specialtyId && !s.IsDeleted);
        if (specialty is not null) return specialty;

        _logger.LogWarning("No se encontró la especialidad activa {SpecialtyId}.", specialtyId);
        throw new EntityNotFoundException("Specialty").WithDetail("specialtyId", "Specialty not found");
    }

    private async Task EnsureLicenseNumberIsAvailable(string licenseNumber, Guid? doctorId = null)
    {
        if (string.IsNullOrWhiteSpace(licenseNumber)) throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR).WithDetail("licenseNumber", "License number is required.");

        var exists = await _persistence.First<Doctor>(d => d.LicenseNumber == licenseNumber && d.IsActive && (!doctorId.HasValue || d.Id != doctorId.Value));

        if (exists is null) return;

        _logger.LogWarning("La matrícula {LicenseNumber} ya pertenece al médico {DoctorId}.", licenseNumber, exists.Id);
        throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR).WithDetail("licenseNumber", "A doctor with this license number already exists.");
    }
}


