using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Dsw2026Tpi.Api.Controllers
{
    [ApiController]
    [Route("api/availabilities")]
    [Authorize(Roles = "ADMINISTRADOR")]
    [EnableRateLimiting("GeneralPolicy")]
    public class AvailabilitiesController : ControllerBase
    {
        private readonly IAvailabilityService _availabilityService;
        public AvailabilitiesController(IAvailabilityService availabilityService)
        {
            _availabilityService = availabilityService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAvailabilities([FromBody] AvailabilityModel.Request request)
        {
            var result = await _availabilityService.CreateAvailabilitiesAsync(request);
            var response = MapToDto(result);
            return Ok(response);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateAvailabilities([FromBody] AvailabilityModel.Request request)
        {
            var result = await _availabilityService.UpdateAvailabilitiesAsync(request);
            var response = MapToDto(result);
            return Ok(response);
        }

        private List<AvailabilityModel.RuleResponse> MapToDto(List<Domain.Entities.AvailabilityRule> rules)
        {
            return rules.Select(r => new AvailabilityModel.RuleResponse(
                r.Id,
                r.DoctorId,
                r.Month,
                r.Year,
                r.DayOfWeek,
                r.StartTime.ToString(@"hh\:mm"),
                r.EndTime.ToString(@"hh\:mm"),
                r.Slots?.Select(s => new AvailabilityModel.SlotResponse(
                    s.Id,
                    s.SlotDate.ToString("yyyy-MM-dd"),
                    s.StartTime.ToString(@"hh\:mm"),
                    s.EndTime.ToString(@"hh\:mm"),
                    s.Status
                )).ToList() ?? new List<AvailabilityModel.SlotResponse>()
            )).ToList();
        }
    }
}