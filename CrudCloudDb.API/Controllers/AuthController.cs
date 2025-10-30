using CrudCloudDb.Application.DTOs.Auth;
using CrudCloudDb.Application.Services.Interfaces;
using CrudCloudDb.Core.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CrudCloudDb.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.RegisterAsync(request);

        if (result.Succeeded)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(request);
        }

        var result = await _authService.LoginAsync(request);
        if (result.Succeeded)
        {
            return Ok(result);
        }

        return Unauthorized(result);
    }
}