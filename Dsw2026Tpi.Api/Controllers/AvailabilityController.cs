using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
namespace Dsw2026Tpi.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AvailabilityController : ControllerBase
    {
        private readonly IAvailabilityService _availabilityService;
        public AvailabilityController(IAvailabilityService availabilityService)
        {
            _availabilityService = availabilityService;
        }
        
        [HttpPost]
        public async Task<IActionResult> CreateAvailabilities([FromBody] AvailabilityModel.Request request)
        {
            await _availabilityService.CreateAvailabilitiesAsync(request);
            return Ok(new AvailabilityModel.Response("Disponibilidades creadas con éxito."));
        }
        
        
        [HttpPut]
        public async Task<IActionResult> UpdateAvailabilities([FromBody] AvailabilityModel.Request request)
        {
            await _availabilityService.UpdateAvailabilitiesAsync(request);
            return Ok(new AvailabilityModel.Response("Disponibilidades actualizadas con éxito."));
        }
        }
    }

