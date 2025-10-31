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
        if (!Guid.TryParse(userIdString, out var userId))
        {
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
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized("Token de usuario inválido.");
        }

        var result = await _userService.UpdateProfileAsync(userId, request);

        if (result.Succeeded)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    // --- 3. CAMBIAR CONTRASEÑA DEL USUARIO ACTUAL ---
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