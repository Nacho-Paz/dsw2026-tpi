using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Domain.Interfaces;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Microsoft.Extensions.Logging;

namespace Dsw2026Tpi.Application.Services;

public class SpecialtyService : ISpecialtyService
{
    private readonly IPersistence _persistence;
    private readonly ILogger<SpecialtyService> _logger;
    public SpecialtyService(IPersistence persistence, ILogger<SpecialtyService> logger)
    {
        _persistence = persistence;
        _logger = logger;
    }

    public async Task<PaginatedResponse<SpecialtyModel>> GetSpecialties(SpecialtyQueryFilter filter)
    {
        var name = filter.name?.Trim();

        if (!string.IsNullOrWhiteSpace(name) && (name.Length < 3 || name.Length > 100))
        {
            _logger.LogWarning("Filtro de nombre inválido para especialidades.");
            throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR).WithDetail("name", $"El nombre debe tener entre 3 y 100 caracteres.");
        }

        var pagedSpecialties = await _persistence.Paginate<Specialty, string>(
            filter.PageSize,
            filter.PageIndex,
            s => !s.IsDeleted && (string.IsNullOrEmpty(filter.name) || s.Name.Contains(filter.name)),
            s => s.Name
        );

        if (pagedSpecialties.Data == null || !pagedSpecialties.Data.Any())
        {
            _logger.LogWarning("No se encontraron especialidades que coincidan con los criterios de búsqueda.");
            throw new EntityNotFoundException("Specialty");
        }

        return new PaginatedResponse<SpecialtyModel>
        {
            pageSize = pagedSpecialties.PageSize,
            pageIndex = pagedSpecialties.PageIndex,
            Total = pagedSpecialties.Total,
            data = pagedSpecialties.Data.Select(MapToModel)
        };
    }

    public async Task<SpecialtyModel> Createspecialty(SpecialtyCreateModel model)
    {
        _logger.LogInformation("Iniciando la creación de una nueva especialidad con el nombre: {Name}", model.Name);

        ValidateSpecialty(model);

        var name = model.Name.Trim();
        var description = model.Description.Trim();

        var existingSpecialty = await _persistence.First<Specialty>(s => s.Name == name && !s.IsDeleted);

        if (existingSpecialty is not null)
        {
            _logger.LogWarning("Intento de creación fallido: la especialidad ya existe.");
            throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR).WithDetail("Name", "Ya existe una especialidad registrada con este nombre.");
        }

        var newSpeciality = new Specialty(name, description);
        await _persistence.Add(newSpeciality);

        _logger.LogInformation("Especialidad creada exitosamente con ID: {Id}", newSpeciality.Id);
        return MapToModel(newSpeciality);
    }

    public async Task<SpecialtyModel> UpdateSpecialty(Guid id, SpecialtyCreateModel model)
    {
        _logger.LogInformation("Iniciando la actualización de la especialidad con ID: {Id}", id);

        ValidateId(id);
        ValidateSpecialty(model);

        var existingSpecialty = await _persistence.First<Specialty>(s => s.Id == id && !s.IsDeleted);

        if (existingSpecialty is null)
        {
            _logger.LogWarning("Especialidad con ID {SpecialtyId} no encontrada.", id);
            throw new EntityNotFoundException("Specialty");
        }

        var name = model.Name.Trim();
        var description = model.Description.Trim();

        var duplicatedSpecialty = await _persistence.First<Specialty>(s => s.Name == name && s.Id != id && !s.IsDeleted);

        if (duplicatedSpecialty is not null)
        {
            _logger.LogWarning("Intento de actualización fallido: la especialidad {SpecialtyId} utiliza un nombre ya existente.", id);
            throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR).WithDetail("Name", "Ya existe una especialidad registrada con este nombre.");
        }


        existingSpecialty.Update(name, description);
        await _persistence.Update(existingSpecialty);

        _logger.LogInformation("Especialidad con ID: {Id} actualizada exitosamente.", id);
        return MapToModel(existingSpecialty);
    }
    public async Task<bool> DeleteSpecialty(Guid id)
    {
        _logger.LogInformation("Iniciando la desactivación de la especialidad con ID: {Id}", id);

        ValidateId(id);

        var existingSpecialty = await _persistence.First<Specialty>(s => s.Id == id && !s.IsDeleted);

        if (existingSpecialty is null)
        {
            _logger.LogWarning("Especialidad con ID {SpecialtyId} no encontrada o ya eliminada.", id);
            throw new EntityNotFoundException("Specialty");
        }

        existingSpecialty.Desactivate();
        await _persistence.Update(existingSpecialty);

        _logger.LogInformation("Especialidad con ID: {Id} desactivada exitosamente.", id);
        return true;
    }

    private static void ValidateSpecialty(SpecialtyCreateModel model)
    {
        if (model is null) throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR);

        if (string.IsNullOrWhiteSpace(model.Name)) throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR).WithDetail("Name", "El nombre de la especialidad es obligatorio.");

        if (model.Name.Trim().Length < 3 || model.Name.Trim().Length > 100) throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR).WithDetail("Name", "El nombre debe tener entre 3 y 100 caracteres.");

        if (string.IsNullOrWhiteSpace(model.Description)) throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR).WithDetail("Description", "La descripción de la especialidad es obligatoria.");

        if (model.Description.Trim().Length < 10 || model.Description.Trim().Length > 100) throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR),
                ErrorCodes.VALIDATION_ERROR).WithDetail("Description", "La descripción debe tener entre 10 y 100 caracteres.");
    }

    private static void ValidateId(Guid id)
    {
        if (id == Guid.Empty) throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR).WithDetail("Id", "El identificador de la especialidad no es válido.");
    }
    private static SpecialtyModel MapToModel(Specialty specialty)
    {
        return new SpecialtyModel
        {
            Id = specialty.Id,
            Name = specialty.Name,
            Description = specialty.Description
        };
    }
}

