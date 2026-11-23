using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Dsw2026Tpi.Api.Controllers;

[ApiController]
[Route("api/doctors")]
[Authorize]
[EnableRateLimiting("GeneralPolicy")]
public class DoctorController : AppController
{
    private readonly IDoctorService _service;

    public DoctorController(IDoctorService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Roles = Roles.Administrator + "," + Roles.Patient)]
    public async Task<IActionResult> GetAll([FromQuery] int pageSize, [FromQuery] int pageIndex, [FromQuery] string? name = null)
    {
        var doctors = await _service.GetAll(pageSize, pageIndex, name);
        return Ok(doctors);
    }

    [HttpGet("{id:guid}/availabilities")]
    [Authorize(Roles = Roles.Administrator + "," + Roles.Patient)]
    public async Task<IActionResult> GetAvailabilities(Guid id)
    {
        var availabilities = await _service.GetDoctorAvailabilities(id);
        if (availabilities == null)
        {
            return NotFound();
        }
        return Ok(availabilities);
    }

    [HttpPost]
    [Authorize(Policy = Policies.AdminPolicy)]
    public async Task<IActionResult> Create([FromBody] DoctorModel.Request model)
    {
        var createdDoctor = await _service.CreateDoctor(model);
        if (createdDoctor == null)
        {
            return BadRequest();
        }
        return Ok(createdDoctor);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminPolicy)]
    public async Task<IActionResult> Update(Guid id, [FromBody] DoctorModel.Request model)
    {
        var updatedDoctor = await _service.UpdateDoctor(id, model);
        if (updatedDoctor == null)
        {
            return NotFound();
        }
        return Ok(updatedDoctor);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminPolicy)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _service.DeleteDoctor(id);
        if (!success)
        {
            return NotFound();
        }
        return Ok("ok");
    }

}
