using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
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
        if (!string.IsNullOrWhiteSpace(name) && (name.Length < 3 || name.Length > 100))
        {
            throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR);
        }
        
            var doctors = await _persistence.Paginate<Doctor, string>(
            pageSize, 
            pageIndex,
           d => d.IsActive && (string.IsNullOrWhiteSpace(name) || d.Name.Contains(name)),
           x => x.Name,
           nameof(Doctor.Speciality));

        return doctors.Map(d => new DoctorModel.Response(d.Id, d.Name, d.LicenseNumber,
            new DoctorModel.SpecialityDto(d.Speciality?.Id, d.Speciality?.Name)));
    }

   public  async Task<List<DoctorModel.AvailabilityResponse>> GetDoctorAvailabilities(Guid doctorId)
    {
        var doctor= await _persistence.First<Doctor>(d => d.Id == doctorId && d.IsActive);
        if (doctor == null)
        {
            throw new EntryPointNotFoundException(nameof(ErrorCodes.ENTITY_NOTFOUND), ErrorCodes.ENTITY_NOTFOUND);
        }
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

   public async Task<DoctorModel.Response?> CreateDoctor(DoctorModel.Request model)
    {

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR);
        }
        if (model.Name.Length < 3 || model.Name.Length > 100)
        {
            throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR);
        }
        var speciality = await _persistence.First<Speciality>(s => s.Id == model.SpecialityId && !s.IsDeleted);
        if (speciality == null)
        {
            throw new EntityNotFoundException(nameof(ErrorCodes.ENTITY_NOTFOUND), ErrorCodes.ENTITY_NOTFOUND);
        }

        var newDoctor = new Doctor(model.Name, model.LicenseNumber, speciality);
        await _persistence.Add(newDoctor);

        return new DoctorModel.Response(
            newDoctor.Id,
            newDoctor.Name,
            newDoctor.LicenseNumber,
            new DoctorModel.SpecialityDto(speciality.Id, speciality.Name));



    }

   public async Task<DoctorModel.Response?> UpdateDoctor(Guid id, DoctorModel.Request model)
    {
        var existingEntity = await _persistence.First<Doctor>(d => d.Id == id && d.IsActive);
        if (existingEntity == null)
        {
            throw new EntityNotFoundException(nameof(ErrorCodes.ENTITY_NOTFOUND), ErrorCodes.ENTITY_NOTFOUND);

        }
        if (string.IsNullOrWhiteSpace(model.Name))
        { 
            throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR);
        }
        if (model.Name.Length < 3 || model.Name.Length > 100)
        {
            throw new ValidationException(nameof(ErrorCodes.VALIDATION_ERROR), ErrorCodes.VALIDATION_ERROR);
        }
        var speciality = await _persistence.First<Speciality>(s => s.Id == model.SpecialityId && !s.IsDeleted);
        if (speciality == null)
        {
            throw new EntityNotFoundException(nameof(ErrorCodes.ENTITY_NOTFOUND), ErrorCodes.ENTITY_NOTFOUND);
        }


        existingEntity.UpdateData(model.Name, model.LicenseNumber, speciality);
        await _persistence.Update(existingEntity);

        return new DoctorModel.Response(
            existingEntity.Id,
            existingEntity.Name,
            existingEntity.LicenseNumber,
            new DoctorModel.SpecialityDto(speciality.Id, speciality.Name));

    }

   public async Task<bool> DeleteDoctor(Guid id)
    {

        var existingEntity = await _persistence.First<Doctor>(d => d.Id == id && d.IsActive);
        if (existingEntity == null)
        {
            throw new EntityNotFoundException(nameof(ErrorCodes.ENTITY_NOTFOUND), ErrorCodes.ENTITY_NOTFOUND);
        }

        existingEntity.Deactivate();
        await _persistence.Update(existingEntity);
        return true;
    }


}


