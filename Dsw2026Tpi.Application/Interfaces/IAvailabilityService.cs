using Dsw2026Tpi.Application.Dtos;

namespace Dsw2026Tpi.Application.Interfaces
{
    public interface IAvailabilityService
    {
        Task<List<AvailabilityModel.RuleResponse>> CreateAvailabilitiesAsync(AvailabilityModel.Request request);
        Task<List<AvailabilityModel.RuleResponse>> UpdateAvailabilitiesAsync(AvailabilityModel.Request request);
    }
}

