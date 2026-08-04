using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Dsw2026Tpi.CrossCutting.Identity;


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
        [Authorize(Roles=Roles.Patient)]
        [EnableRateLimiting("PatientBookingPolicy")]
        public async Task<IActionResult> Create([FromBody] AppointmentModel.Request request)
        {
            var appointmentCreated = await _appointmentService.CreateAppointmentAsync(request);
            return Created(string.Empty, appointmentCreated);
        }

        [HttpGet("patient")]
        [Authorize(Roles = Roles.Patient + ", " + Roles.Administrator)]
        public async Task<IActionResult> GetPatientAppointments([FromQuery] long dni)
        {
            var appointments = await _appointmentService.GetActiveAppointmentsByPatientAsync(dni);

            return Ok(appointments);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = Roles.Patient + "," + Roles.Administrator)]
        public async Task<IActionResult> Cancel(Guid id)
        {
            await _appointmentService.CancelAppointmentAsync(id);

            return Ok("ok");
        }

        [HttpGet]
        [Authorize(Roles = Roles.Administrator)]
        public async Task<IActionResult> GetByDate([FromQuery] string date)
        {
            var result = await _appointmentService.GetAppointmentsByDateAsync(date);
            return Ok(result);
        }

        [HttpGet("search")]
        [Authorize(Roles = Roles.Administrator)]
        public async Task<IActionResult> Search(
            [FromQuery] int pageSize,
            [FromQuery] int pageIndex,
            [FromQuery] Guid? specialtyId,
            [FromQuery] Guid? doctorId,
            [FromQuery] long? dni,
            [FromQuery] string date)
        {
            var result = await _appointmentService.SearchAppointmentsAsync(pageSize, pageIndex, specialtyId, doctorId, dni, date);
            return Ok(result);
        }

    }
}
