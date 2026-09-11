using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Dsw2026Tpi.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthenticationController : AppController
{
    private readonly IAuthenticationService _authenticationService;

    public AuthenticationController(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    //TODO: Libre para todos hasta crear uno más privado
    [HttpPost("admin/register")]
    //[Authorize(Policy = Policies.AdminPolicy)]
    //[EnableRateLimiting("GeneralPolicy")]
    public async Task<IActionResult> Register([FromBody] RegisterModel.Request request)
    {
        var result = await _authenticationService.Register(request);
        return Ok(result.Email);
    }

    [HttpPost("admin/login")]
    [AllowAnonymous]
    [EnableRateLimiting("AdminLogin")]
    public async Task<IActionResult> LoginAdmin([FromBody] LoginAdminModel.Request request)
    {
        var result = await _authenticationService.LoginAdmin(request);
        return Ok(result);
    }

    [HttpPost("patient/login")]
    [AllowAnonymous]
    [EnableRateLimiting("PatientLogin")]
    public async Task<IActionResult> LoginPatient(
        [FromBody] LoginPatientModel.Request request)
    {
        var result = await _authenticationService.LoginPatient(request);
        return Ok(result);
    }
}
