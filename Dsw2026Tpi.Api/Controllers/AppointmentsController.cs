using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using static Dsw2026Tpi.Application.Dtos.AppointmentModel;

namespace Dsw2026Tpi.Api.Controllers
{
    [ApiController]
    [Route("api/appointments")]
    [Authorize]
    [EnableRateLimiting("GeneralPolicy")]
    public class AppointmentsController : ControllerBase
    { 
        private readonly IAppointmentService _appointmentService;
        public AppointmentsController(IAppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }

        [HttpPost]
        [Authorize(Roles = "PACIENTE")]
        [EnableRateLimiting("PatientBookingPolicy")]
        public async Task<IActionResult> Create([FromBody] AppointmentModel.Request request)
        {
            var appointmentCreated = await _appointmentService.CreateAppointmentAsync(request);
            return Created(string.Empty, appointmentCreated);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "PACIENTE, ADMINISTRADOR")]
        public async Task<IActionResult> Cancel(Guid id)
        {
            await _appointmentService.CancelAppointmentAsync(id);

            return Ok("Ok");
        }

        [HttpGet("patient")]
        [Authorize(Roles = "PACIENTE, ADMINISTRADOR")]
        public async Task<IActionResult> GetPatientAppointments([FromQuery] long dni)
        {
            var appointments = await _appointmentService.GetActiveAppointmentsByPatientAsync(dni);

            return Ok(appointments);
        }
    }
}
