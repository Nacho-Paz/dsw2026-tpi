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
    public class SpecialityService : ISpecialtyService
    {
        private readonly IPersistence _persistence;
        public SpecialityService(IPersistence _persistence)
        {
            _persistence = _persistence;
        }
                    
        public async Task< Pagination<SpecialtyModel>>GetSpecialties(SpecialtyQueryFilter filter)
        {
            var specialitiesList = await _persistence.GetFiltered<Specialty>(
                s => !s.IsDeleted && (string.IsNullOrEmpty(filter.name) || s.Name.Contains(filter.name))); ;


            var query = specialitiesList.ToList();


            var totalRecords =query.Count();
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
            var newSpeciality = new Specialty(model.Name, model.Description);
            await _persistence.Add(newSpeciality);
            return new SpecialtyModel
            {
                Id = newSpeciality.Id,
                Name = newSpeciality.Name,
                Description = newSpeciality.Description
            };

        }
        public async Task<SpecialtyModel>UpdateSpecialty(Guid id, SpecialtyCreateModel model)
        {
            var existingEntity = await _persistence.First<Specialty>(s => s.Id == id);
            if (existingEntity == null || existingEntity.IsDeleted) return null;

            existingEntity.Name = model.Name;
            existingEntity.Description = model.Description;

            await _persistence.Update(existingEntity);
            return new SpecialtyModel
            {
                Id = existingEntity.Id,
                Name = existingEntity.Name,
                Description = existingEntity.Description
            };
        }
        public async Task <bool> DeleteSpecialty(Guid id)
        {
            var existingEntity = await _persistence.First<Specialty>(s => s.Id == id);
            if (existingEntity == null || existingEntity.IsDeleted) return false;

            //existingEntity.desactivate(); TODO: Ver que onda esto
            await _persistence.Update(existingEntity);
            return true;
        }

    }
}

