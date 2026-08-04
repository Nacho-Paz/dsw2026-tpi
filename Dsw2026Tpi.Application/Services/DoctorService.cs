using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.Extensions.Logging;

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
        if (!string.IsNullOrWhiteSpace(name) && (name.Length < 3 || name.Length > 100))
        {
            _logger.LogWarning("Filtro de nombre inválido en GetAll: {Name}", name);
            throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR);
        }

        var doctors = await _persistence.Paginate<Doctor, string>(
        pageSize,
        pageIndex,
        d => d.IsActive && (string.IsNullOrWhiteSpace(name) || d.Name.Contains(name)),
        x => x.Name,
        nameof(Doctor.Speciality));

        return doctors.Map(d => new DoctorModel.Response(d.Id, d.Name, d.LicenseNumber,
            new DoctorModel.SpecialityDto(d.Speciality?.Id, d.Speciality?.Name)));
    }

    public async Task<List<DoctorModel.AvailabilityResponse>> GetDoctorAvailabilities(Guid doctorId)
    {
        var doctor = await _persistence.First<Doctor>(d => d.Id == doctorId && d.IsActive);
        if (doctor == null)
        {
            _logger.LogWarning("Médico con ID {DoctorId} no encontrado para consultar disponibilidades.", doctorId);
            throw new EntityNotFoundException("Doctor");
        }
        var now = DateTime.Now;
        var rules = await _persistence.GetFiltered<AvailabilityRule>(
            r => r.DoctorId == doctorId && r.Month == now.Month && r.Year == now.Year && !r.Deleted,
            "Slots");

        if (rules == null || !rules.Any())
        {
            return new List<DoctorModel.AvailabilityResponse>();
        }

        return rules.Select(r => new DoctorModel.AvailabilityResponse(
            r.Id,
            r.DayOfWeek.ToString(),
            r.StartTime.ToString(@"hh\:mm"),
            r.EndTime.ToString(@"hh\:mm"))).ToList();

    }

    public async Task<DoctorModel.Response?> CreateDoctor(DoctorModel.Request model)
    {
        _logger.LogInformation("Iniciando la creación de un nuevo médico: {Name}", model.Name);

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            _logger.LogWarning("Intento de creación de médico fallido por validación inválida.");
            throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR);
        }
        if (model.Name.Length < 3 || model.Name.Length > 100)
        {
            _logger.LogWarning("Intento de creación de médico fallido por validación inválida.");
            throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR);
        }
        var speciality = await _persistence.First<Specialty>(s => s.Id == model.SpecialityId && !s.IsDeleted);
        if (speciality == null)
        {
            _logger.LogWarning("Especialidad con ID {SpecialityId} no encontrada al crear médico.", model.SpecialityId);
            throw new EntityNotFoundException("Doctor");
        }

        var newDoctor = new Doctor(model.Name, model.LicenseNumber, speciality);
        await _persistence.Add(newDoctor);
        _logger.LogInformation("Médico creado exitosamente con ID {DoctorId}", newDoctor.Id);

        return new DoctorModel.Response(
            newDoctor.Id,
            newDoctor.Name,
            newDoctor.LicenseNumber,
            new DoctorModel.SpecialityDto(speciality.Id, speciality.Name));



    }

    public async Task<DoctorModel.Response?> UpdateDoctor(Guid id, DoctorModel.Request model)
    {
        _logger.LogInformation("Iniciando la actualización del médico con ID: {DoctorId}", id);
        var existingEntity = await _persistence.First<Doctor>(d => d.Id == id && d.IsActive);
        if (existingEntity == null)
        {
            _logger.LogWarning("No se encontró el médico con ID {DoctorId} para actualizar.", id);

            throw new EntityNotFoundException("Doctor");
        }

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            _logger.LogWarning("Validación fallida: El nombre del médico es nulo o vacío para el ID {DoctorId}.", id);
            throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR);
        }
        if (model.Name.Length < 3 || model.Name.Length > 100)
        {
            _logger.LogWarning("Validación fallida: La longitud del nombre no es válida para el ID {DoctorId}.", id);

            throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR);
        }
        var speciality = await _persistence.First<Specialty>(s => s.Id == model.SpecialityId && !s.IsDeleted);
        if (speciality == null)
        {
            _logger.LogWarning("La especialidad con ID {SpecialityId} no fue encontrada al intentar actualizar el médico {DoctorId}.", model.SpecialityId, id);
            throw new EntityNotFoundException("Doctor");
        }


        existingEntity.UpdateData(model.Name, model.LicenseNumber, speciality);
        await _persistence.Update(existingEntity);
        _logger.LogInformation("Médico con ID {DoctorId} actualizado exitosamente.", id);
        return new DoctorModel.Response(
            existingEntity.Id,
            existingEntity.Name,
            existingEntity.LicenseNumber,
            new DoctorModel.SpecialityDto(speciality.Id, speciality.Name));

    }

    public async Task<bool> DeleteDoctor(Guid id)
    {
        _logger.LogInformation("Iniciando la desactivación del médico con ID: {DoctorId}", id);

        var existingEntity = await _persistence.First<Doctor>(d => d.Id == id && d.IsActive);
        if (existingEntity == null)
        {
            _logger.LogWarning("No se encontró el médico activo con ID {DoctorId} para eliminar.", id);
            throw new EntityNotFoundException("Doctor");
        }

        existingEntity.Deactivate();
        await _persistence.Update(existingEntity);
        _logger.LogInformation("Médico con ID {DoctorId} desactivado exitosamente.", id);
        return true;
    }


}


