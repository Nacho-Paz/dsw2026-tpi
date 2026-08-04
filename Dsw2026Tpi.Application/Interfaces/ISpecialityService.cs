using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Interfaces
{
    public interface ISpecialityService
    {
        Task<Pagination<SpecialityModel>> GetSpecialities(SpecialityQueryFilter filter);
        Task<SpecialityModel> Createspeciality(SpecialityCreateModel model);
        Task<SpecialityModel> UpdateSpeciality(Guid id, SpecialityCreateModel model);
        Task<bool> DeleteSpeciality(Guid id);


    }
}


