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
           d => d.IsActive && (string.IsNullOrWhiteSpace(name) || d.Name.Contains(name)),
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

    async Task<DoctorModel.Response?> CreateDoctor(DoctorModel.Request model)
    {
        var speciality = await _persistence.First<Speciality>(s => s.Id == model.SpecialityId && !s.IsDeleted);
        if (speciality == null)
        {
            return null;
        }

        var newDoctor = new Doctor(model.Name, model.LicenseNumber, speciality);
        await _persistence.Add(newDoctor);

        return new DoctorModel.Response(
            newDoctor.Id,
            newDoctor.Name,
            newDoctor.LicenseNumber,
            new DoctorModel.SpecialityDto(speciality.Id, speciality.Name));



    }

    async Task<DoctorModel.Response?> UpdateDoctor(Guid id, DoctorModel.Request model)
    {
        var existingEntity = await _persistence.First<Doctor>(d => d.Id == id && d.IsActive);
        if (existingEntity == null)
        {
            return null;
        }

        var speciality = await _persistence.First<Speciality>(s => s.Id == model.SpecialityId && !s.IsDeleted);
        if (speciality == null)
        {
            return null;
        }

        existingEntity.UpdateData(model.Name, model.LicenseNumber, speciality);
        await _persistence.Update(existingEntity);

        return new DoctorModel.Response(
            existingEntity.Id,
            existingEntity.Name,
            existingEntity.LicenseNumber,
            new DoctorModel.SpecialityDto(speciality.Id, speciality.Name));

    }

    async Task<bool> DeleteDoctor(Guid id)
    {

        var existingEntity = await _persistence.First<Doctor>(d => d.Id == id && d.IsActive);
        if (existingEntity == null)
        {
            return false;
        }

        existingEntity.Deactivate();
        await _persistence.Update(existingEntity);
        return true;
    }


}


}
