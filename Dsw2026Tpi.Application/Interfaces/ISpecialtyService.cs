using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Interfaces;

public interface ISpecialtyService
{
    Task<Pagination<SpecialtyModel>> GetSpecialties(SpecialtyQueryFilter filter);
    Task<SpecialtyModel> Createspecialty(SpecialtyCreateModel model);
    Task<SpecialtyModel> UpdateSpecialty(Guid id, SpecialtyCreateModel model);
    Task<bool> DeleteSpecialty(Guid id);
}


