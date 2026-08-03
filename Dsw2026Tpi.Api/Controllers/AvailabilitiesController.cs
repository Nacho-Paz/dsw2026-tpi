using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
<<<<<<< HEAD
using Microsoft.AspNetCore.Mvc;
namespace Dsw2026Tpi.Api.Controllers
{
    [ApiController]
    [Route("api/availability")]
=======
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Linq;

namespace Dsw2026Tpi.Api.Controllers
{
    [ApiController]
    [Route("api/availabilities")]
    [Authorize(Roles = "ADMINISTRADOR")]
    [EnableRateLimiting("GeneralPolicy")]
>>>>>>> modulo/disponibilidad
    public class AvailabilitiesController : ControllerBase
    {
        private readonly IAvailabilityService _availabilityService;
        public AvailabilitiesController(IAvailabilityService availabilityService)
        {
            _availabilityService = availabilityService;
        }
<<<<<<< HEAD

        [HttpPost]
        public async Task<IActionResult> CreateAvailabilities([FromBody] AvailabilityModel.Request request)
        {
            await _availabilityService.CreateAvailabilitiesAsync(request);
            return Ok(new AvailabilityModel.Response("Disponibilidades creadas con éxito."));
=======
        
        [HttpPost]
        public async Task<IActionResult> CreateAvailabilities([FromBody] AvailabilityModel.Request request)
        {
            var result = await _availabilityService.CreateAvailabilitiesAsync(request);
            var response = MapToDto(result);
            return Ok(response);
>>>>>>> modulo/disponibilidad
        }
        
        
        [HttpPut]
        public async Task<IActionResult> UpdateAvailabilities([FromBody] AvailabilityModel.Request request)
        {
<<<<<<< HEAD
            await _availabilityService.UpdateAvailabilitiesAsync(request);
            return Ok(new AvailabilityModel.Response("Disponibilidades actualizadas con éxito."));
        }
        }
    }
=======
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
>>>>>>> modulo/disponibilidad

