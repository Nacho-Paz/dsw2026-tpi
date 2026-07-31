using System.Linq;
using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Domain.Interfaces;
using Dsw2026Tpi.Domain.Entities;
using System.Threading.Tasks;
using Dsw2026Tpi.Data;
using System;

namespace Dsw2026Tpi.Application.Services
{
    public class SpecialityService : ISpecialityService
    {
        private readonly ISpecialityRepository _repository;
        public SpecialityService(ISpecialityRepository repository)
        {
            _repository = repository;
        }

        public async Task< Pagination<SpecialityModel>>GetSpecialities(SpecialityQueryFilter filter)
        {
            var specialitiesList = await _repository.GetAllAsync();
            var query = specialitiesList.Where(s => !s.IsDeleted);
          

            if (!string.IsNullOrEmpty(filter.name))
            {
                query = query.Where(s => s.Name.Contains(filter.name, StringComparison.OrdinalIgnoreCase));
            }
            var totalRecords = query.Count();
            if (totalRecords == 0)
            {
                return Pagination<SpecialityModel>.Empty;
            }

            var pagedData = query
                .Skip((filter.PageIndex - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            var paginationResult = new Pagination<Speciality>(
                filter.PageSize,
                filter.PageIndex,
                totalRecords,
                pagedData

                );

            return paginationResult.Map(s => new SpecialityModel
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description
            });

        }

        public async Task<SpecialityModel> Createspeciality(SpecialityCreateModel model)
        {
            var newSpeciality = new Speciality(model.Name, model.Description);
            await _repository.AddAsync(newSpeciality);
            return new SpecialityModel
            {
                Id = newSpeciality.Id,
                Name = newSpeciality.Name,
                Description = newSpeciality.Description
            };

        }
        public async Task<SpecialityModel>UpdateSpeciality(Guid id, SpecialityCreateModel model)
        {
            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null || existingEntity.IsDeleted) return null;

            existingEntity.Name = model.Name;
            existingEntity.Description = model.Description;

            await _repository.UpdateAsync(existingEntity);
            return new SpecialityModel
            {
                Id = existingEntity.Id,
                Name = existingEntity.Name,
                Description = existingEntity.Description
            };
        }
        public async Task <bool> DeleteSpeciality(Guid id)
        {
            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity == null || existingEntity.IsDeleted) return false;

            existingEntity.desactivate();
            await _repository.UpdateAsync(existingEntity);
            return true;
        }

    }
}

