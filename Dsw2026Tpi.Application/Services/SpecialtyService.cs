using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Domain.Interfaces;
using Dsw2026Tpi.Domain.Entities;


using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Microsoft.Extensions.Logging;

namespace Dsw2026Tpi.Application.Services
{
    public class SpecialtyService : ISpecialtyService
    {
        private readonly IPersistence _persistence;
        private readonly ILogger<SpecialtyService> _logger;
        public SpecialtyService(IPersistence persistence, ILogger<SpecialtyService> logger)
        {
            _persistence = persistence;
            _logger = logger;
        }

        public async Task<Pagination<SpecialtyModel>> GetSpecialties(SpecialtyQueryFilter filter)

        {
            var specialitiesList = await _persistence.GetFiltered<Specialty>(
                s => !s.IsDeleted && (string.IsNullOrEmpty(filter.name) || s.Name.Contains(filter.name))); 


            if (specialitiesList == null || !specialitiesList.Any())
            {
                _logger.LogWarning("No se encontraron especialidades que coincidan con los criterios de búsqueda.");
                throw new EntityNotFoundException(nameof(ErrorCodes.ENTITY_NOTFOUND), ErrorCodes.ENTITY_NOTFOUND);
            }

            var query = specialitiesList.ToList();


            var totalRecords = query.Count();
            if (totalRecords == 0)
            {
                return Pagination<SpecialtyModel>.Empty;
            }

            var pagedData = query
                .Skip((filter.PageIndex - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            var paginationResult = new Pagination<Specialty>(
                filter.PageSize,
                filter.PageIndex,
                totalRecords,
                pagedData

                );

            return paginationResult.Map(s => new SpecialtyModel
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description
            });

        }

        public async Task<SpecialtyModel> Createspecialty(SpecialtyCreateModel model)

        {
            _logger.LogInformation("Iniciando la creación de una nueva especialidad con el nombre: {Name}", model.Name);

            if (model.Name==null || model.Description == null){
                _logger.LogWarning("Error de validación: Nombre o descripción nulos.");
                throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR);
            }
            if(model.Name.Length < 3 || model.Name.Length > 100)
            {
                _logger.LogWarning("Error de validación: El nombre debe tener entre 3 y 100 caracteres.");
                throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR);
            }
            if (model.Description.Length < 10 || model.Description.Length > 100)
            {
                _logger.LogWarning("Error de validación: La descripción debe tener entre 10 y 100 caracteres.");
                throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR);
            }

    
            var newSpeciality = new Specialty(model.Name, model.Description);
            await _persistence.Add(newSpeciality);
            _logger.LogInformation("Especialidad creada exitosamente con ID: {Id}", newSpeciality.Id);

            return new SpecialtyModel
            {
                Id = newSpeciality.Id,
                Name = newSpeciality.Name,
                Description = newSpeciality.Description
            };

        }
        public async Task<SpecialtyModel> UpdateSpecialty(Guid id, SpecialtyCreateModel model)
        {

            _logger.LogInformation("Iniciando la actualización de la especialidad con ID: {Id}", id);

            if (model.Name == null || model.Description == null)
            {
                _logger.LogWarning("Error de validación: Nombre o descripción nulos.");
                throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR);
            }
            if (model.Name.Length < 3 || model.Name.Length > 100)
            {
                _logger.LogWarning("Error de validación: El nombre debe tener entre 3 y 100 caracteres.");
                throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR);        
            }
            if (model.Description.Length < 10 || model.Description.Length > 100)
            {
                _logger.LogWarning("Error de validación: La descripción debe tener entre 10 y 100 caracteres.");
                throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR);
            }

            var existingEntity = await _persistence.First<Specialty>(s => s.Id == id);
            if (existingEntity == null || existingEntity.IsDeleted) return null;

            existingEntity.Name = model.Name;
            existingEntity.Description = model.Description;

            await _persistence.Update(existingEntity);
            _logger.LogInformation("Especialidad con ID: {Id} actualizada exitosamente.", id);

            return new SpecialtyModel
            {
                Id = existingEntity.Id,
                Name = existingEntity.Name,
                Description = existingEntity.Description
            };
        }
        public async Task<bool> DeleteSpecialty(Guid id)
        {
            _logger.LogInformation("Iniciando la desactivación de la especialidad con ID: {Id}", id);

            var existingEntity = await _persistence.First<Specialty>(s => s.Id == id);
            if (existingEntity == null || existingEntity.IsDeleted)
            {
                _logger.LogWarning("Especialidad con ID: {Id} no encontrada o ya eliminada.", id);
                throw new EntityNotFoundException(nameof(ErrorCodes.ENTITY_NOTFOUND), ErrorCodes.ENTITY_NOTFOUND);

            }

            existingEntity.Desactivate();
            await _persistence.Update(existingEntity);
            _logger.LogInformation("Especialidad con ID: {Id} desactivada exitosamente.", id);
            return true;
        }

    }
}

