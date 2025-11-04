using System.Security.Claims;
using CrudCloudDb.Application.DTOs.User;
using CrudCloudDb.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrudCloudDb.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly ILogger<UsersController> _logger;
    private readonly IUserService _userService;
    public UsersController(ILogger<UsersController> logger,  IUserService userService)
    {
        _logger = logger;
        _userService = userService;
    }



    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("Petición recibida para [GET /api/Users/me] por el usuario con ID: {UserId}", userIdString);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            _logger.LogWarning("El token para [GET /api/Users/profile] contenía un ID de usuario con formato inválido: {UserIdString}", userIdString);
            return Unauthorized("Token invalido");
        }

        var result = await _userService.GetProfileAsync(userId);
        if (result.Succeeded)
        {
            return Ok(result);
        }

        return NotFound();
    }
    
    
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("Petición recibida para [PUT /api/users/me] por el usuario con ID: {UserId}", userIdString);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            _logger.LogWarning("El token para [PUT /api/users/me] contenía un ID de usuario con formato inválido: {UserIdString}", userIdString);
            return Unauthorized("Token de usuario inválido.");
        }

        var result = await _userService.UpdateProfileAsync(userId, request);

        if (result.Succeeded)
        {
            return Ok(result); 
        }

        return BadRequest(result);
    }
    
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangeMyPassword([FromBody] ChangePasswordRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized("Token de usuario inválido.");
        }

        var result = await _userService.ChangePasswordAsync(userId, request);

        if (result.Succeeded)
        {
            return Ok(result);
        }
            
        return BadRequest(result);
    }
    
    
}