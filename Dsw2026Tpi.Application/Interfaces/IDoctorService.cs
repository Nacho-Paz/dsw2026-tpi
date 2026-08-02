using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IDoctorService
{
    Task<Pagination<DoctorModel.Response>> GetAll(int pageSize, int pageIndex, string? name = null);
    Task<List<DoctorModel.AvailabilityResponse>> GetDoctorAvailabilities(Guid doctorId);
    Task<DoctorModel.Response?> CreateDoctor(DoctorModel.Request model);
    Task<DoctorModel.Response?> UpdateDoctor(Guid id, DoctorModel.Request model);
    Task<bool> DeleteDoctor(Guid id);
}
