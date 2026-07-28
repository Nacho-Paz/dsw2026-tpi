using System.Linq;
using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Domain.Interfaces;
using Dsw2026Tpi.Domain.Entities;
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

        public PaginatedResponse<SpecialityModel> GetSpecialities(SpecialityQueryFilter filter)
        {
            var query = _repository.GetAll().Where(s => !s.IsDeleted);
            if (!string.IsNullOrEmpty(filter.SearchText))
            {
                query = query.Where(s => s.Name.Contains(filter.SearchText));
            }
            var totalRecords = query.Count();
            if (totalRecords == 0)
            {
                return Pagination<SpecialityModel>.Empty;
            }

        }

        SpecialityModel Createspeciality(SpecialityCreateModel model)
        {

        }
    }
}
