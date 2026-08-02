using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Application.Services;

public class DoctorService : IDoctorService
{
    private readonly IPersistence _persistence;

    public DoctorService(IPersistence persistence)
    {
        _persistence = persistence;
    }

    public async Task<Pagination<DoctorModel.Response>> GetAll(int pageSize, int pageIndex, string? name = null)
    {
        var doctors = await _persistence.Paginate<Doctor, string>(
            pageSize, 
            pageIndex,
            d => string.IsNullOrWhiteSpace(name) ||d.Name.Contains(name),
            x => x.Name, 
            nameof(Doctor.Speciality));

        return doctors.Map(d => new DoctorModel.Response(d.Id, d.Name, d.LicenseNumber,
            new DoctorModel.SpecialityDto(d.Speciality?.Id, d.Speciality?.Name)));
    }

    async Task<List<DoctorModel.AvailabilityResponse>> GetDoctorAvailabilities(Guid doctorId)
    {
        var doctor= await _persistence.First<Doctor>(d => d.Id == doctorId && d.IsActive);
        if (doctor == null) return null;
        var now= DateTime.Now;
        var rules = await _persistence.GetFiltered<AvailabilityRule>(
     r => r.DoctorId == doctorId && r.Month == now.Month && r.Year == now.Year && !r.Deleted,
     "Slots");

        if (rules == null || !rules.Any())
        {
            return new List<DoctorModel.AvailabilityResponse>();
        }

        return rules.Select(r => new DoctorModel.AvailabilityResponse(
            r.Id,
            r.DayOfWeek,
            r.StartTime.ToString(@"hh\:mm"),
            r.EndTime.ToString(@"hh\:mm"))).ToList();

    }
    

}
