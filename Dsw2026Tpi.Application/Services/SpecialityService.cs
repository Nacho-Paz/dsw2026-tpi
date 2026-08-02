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
        private readonly IPersistence _persistence;
        public SpecialityService(IPersistence _persistence)
        {
            _persistence = _persistence;
        }

        public async Task<Pagination<SpecialityModel>> GetSpecialities(SpecialityQueryFilter filter)
        {
            var specialitiesList = await _persistence.GetFiltered<Speciality>(
                s => !s.IsDeleted && (string.IsNullOrEmpty(filter.name) || s.Name.Contains(filter.name))); ;


            var query = specialitiesList.ToList();


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
            await _persistence.Add(newSpeciality);
            return new SpecialityModel
            {
                Id = newSpeciality.Id,
                Name = newSpeciality.Name,
                Description = newSpeciality.Description
            };

        }
        public async Task<SpecialityModel> UpdateSpeciality(Guid id, SpecialityCreateModel model)
        {
            var existingEntity = await _persistence.First<Speciality>(s => s.Id == id);
            if (existingEntity == null || existingEntity.IsDeleted) return null;

            existingEntity.Name = model.Name;
            existingEntity.Description = model.Description;

            await _persistence.Update(existingEntity);
            return new SpecialityModel
            {
                Id = existingEntity.Id,
                Name = existingEntity.Name,
                Description = existingEntity.Description
            };
        }
        public async Task<bool> DeleteSpeciality(Guid id)
        {
            var existingEntity = await _persistence.First<Speciality>(s => s.Id == id);
            if (existingEntity == null || existingEntity.IsDeleted) return false;

            existingEntity.desactivate();
            await _persistence.Update(existingEntity);
            return true;
        }

    }
}

