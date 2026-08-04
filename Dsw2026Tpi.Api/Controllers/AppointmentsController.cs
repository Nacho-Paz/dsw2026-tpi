using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Dsw2026Tpi.CrossCutting.Identity;


namespace Dsw2026Tpi.Api.Controllers;

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
    [Authorize(Roles = Roles.Patient)]
    [EnableRateLimiting("PatientBooking")]
    //[ProducesResponseType(StatusCodes.Status201Created)]
    //[ProducesResponseType(StatusCodes.Status400BadRequest)]
    //[ProducesResponseType(StatusCodes.Status401Unauthorized)]
    //[ProducesResponseType(StatusCodes.Status403Forbidden)]
    //[ProducesResponseType(StatusCodes.Status409Conflict)]
    //[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Create([FromBody] AppointmentModel.Request request)
    {
        var appointmentCreated = await _appointmentService.CreateAppointmentAsync(request);
        return Created(string.Empty, appointmentCreated);
    }

    [HttpGet("patient")]
    [Authorize(Roles = Roles.Patient + ", " + Roles.Administrator)]
    //[ProducesResponseType(StatusCodes.Status200OK)]
    //[ProducesResponseType(StatusCodes.Status401Unauthorized)]
    //[ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPatientAppointments([FromQuery] long dni)
    {
        var appointments = await _appointmentService.GetActiveAppointmentsByPatientAsync(dni);

        return Ok(appointments);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Patient + "," + Roles.Administrator)]
    //[ProducesResponseType(StatusCodes.Status200OK)]
    //[ProducesResponseType(StatusCodes.Status404NotFound)]
    //[ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await _appointmentService.CancelAppointmentAsync(id);

        return Ok("ok");
    }

    [HttpGet] ///Esta mal
    [Authorize(Roles = Roles.Administrator)]
    //[ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByDate([FromQuery] string date)
    {
        var result = await _appointmentService.GetAppointmentsByDateAsync(date);
        return Ok(result);
    }

    [HttpGet("search")]
    [Authorize(Roles = Roles.Administrator)]
    //[ProducesResponseType(StatusCodes.Status200OK)]
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
