using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateAvailabilities([FromBody] AvailabilityModel.Request request)
        {
            var result = await _availabilityService.UpdateAvailabilitiesAsync(request);
            return Ok(result);
        }
    }
}