using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IDoctorService
{
    Task<Pagination<DoctorModel.Response>> GetAll(int pageSize, int pageIndex, string? name = null);
    Task<IEnumerable<object>> GetDoctorAvailabilitiesAsync(Guid doctorId);
    Task<DoctorModel.Response> CreateDoctorAsync(DoctoCreateModel model);
    Task<DoctorModel.Response> UpdateDoctorAsync(Guid id, DoctorCreateModel model);
    Task<bool> DeleteDoctorAsync(Guid id);
}
