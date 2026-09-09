using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Dsw2026Tpi.Application.Services;

public class PatientService : IPatientService
{
    private readonly IPersistence _persistence;
    private readonly ILogger<PatientService> _logger;

    public PatientService(IPersistence persistence, ILogger<PatientService> logger)
    {
        _persistence = persistence;
        _logger = logger;
    }
    public async Task<Patient?> GetByDni(string dni)
    {
        return await _persistence.First<Patient>(p => p.Dni == dni && !p.Deleted);
    }

    public async Task<Patient?> GetByUserId(Guid userId)
    {
        return await _persistence.First<Patient>(p => p.UserId == userId && !p.Deleted);
    }

    public async Task CreatePatient(Guid UserId, string dni)
    {
        var p = new Patient(UserId, dni);
        await _persistence.Add(p);
    }
}
