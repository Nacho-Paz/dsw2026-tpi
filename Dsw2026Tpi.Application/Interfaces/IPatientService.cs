
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IPatientService
{
    Task<Patient?> GetByDni(string dni);
    Task CreatePatient(Guid UserId, string dni);
    Task<Patient?> GetByUserId(Guid userId);
}
