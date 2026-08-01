using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Dsw2026Tpi.Application.Dtos.AppointmentModel;

namespace Dsw2026Tpi.Api.Controllers
{
    [ApiController]
    [Route("api/appointments")]
    [Authorize]
    public class AppointmentsController : ControllerBase
    { 
        private readonly IAppointmentService _appointmentService;
        public AppointmentsController(IAppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }

        [HttpPost]
        [Authorize(Roles = "PACIENTE")]
        public async Task<IActionResult> Create([FromBody] AppointmentModel.Request request)
        {
            await _appointmentService.CreateAppointmentAsync(request);
            return Created(string.Empty, new AppointmentModel.Response("Turno reservado con éxito."));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "PACIENTE, ADMINISTRADOR")]
        public async Task<IActionResult> Cancel(Guid id)
        {
            await _appointmentService.CancelAppointmentAsync(id);

            return Ok(new AppointmentModel.Response("Turno cancelado correctamente."));
        }

        [HttpGet("patient")]
        [Authorize(Roles = "PACIENTE, ADMINISTRADOR")]
        public async Task<IActionResult> GetPatientAppointments([FromQuery] Guid patientId)
        {
            var appointments = await _appointmentService.GetActiveAppointmentsByPatientAsync(patientId);

            return Ok(appointments);
        }
    }
}
