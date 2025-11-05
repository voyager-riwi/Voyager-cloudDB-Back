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
    private readonly IWebhookService _webhookService;
    public UsersController(ILogger<UsersController> logger,  IUserService userService, IWebhookService webhookService)
    {
        _logger = logger;
        _userService = userService;
        _webhookService = webhookService;
    }



    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("Petición recibida para [GET /api/Users/me] por el usuario con ID: {UserId}", userIdString);
            if (!Guid.TryParse(userIdString, out var userId))
            {
                _logger.LogWarning("El token para [GET /api/Users/profile] contenía un ID de usuario con formato inválido: {UserIdString}", userIdString);
                return Unauthorized("Token invalido");
            }
            
            throw new InvalidOperationException("Error de prueba para notificación en Discord!");

            var result = await _userService.GetProfileAsync(userId);
            if (result.Succeeded)
            {
                return Ok(result);
            }

            return NotFound();
        }
        catch (Exception e)
        {
            _logger.LogError(e , "Ocurrio un error no controlado en GetMyFrofile");
            await _webhookService.SendErrorNotificationAsync(e, "Error al intentar obtener el perfil del usuario (GetMyProfile).");
            return StatusCode(500, new { message = "Ocurrió un error interno en el servidor. El equipo ha sido notificado." });
        }
        
            
    }
    
    
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogError("Error de validación del modelo para [PUT /api/users/profile]. Errores: {@ModelStateErrors}", ModelState.Values.SelectMany(v => v.Errors));
            return BadRequest(ModelState);
        }

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        _logger.LogDebug("Iniciando actualización de perfil para usuario {UserIdString}. Datos de la petición: {@RequestData}", userIdString, request);

        if (!Guid.TryParse(userIdString, out var userId))
        {
            _logger.LogWarning("El token para [PUT /api/users/profile] contenía un ID de usuario con formato inválido: {UserIdString}", userIdString);
            return Unauthorized("Token de usuario inválido.");
        }

        var result = await _userService.UpdateProfileAsync(userId, request);

        if (result.Succeeded)
        {
            _logger.LogInformation("Perfil del usuario {UserId} actualizado exitosamente.", userId);
            return Ok(result); 
        }

        _logger.LogWarning("Fallo en la actualización del perfil para el usuario {UserId}. Razón: {FailureReason}", userId, result.Message);
        return BadRequest(result);
    }
    
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangeMyPassword([FromBody] ChangePasswordRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogError("Error de validación del modelo para [POST /api/users/profile]. Errores: {@ModelStateErrors}", ModelState.Values.SelectMany(v => v.Errors));
            return BadRequest(ModelState);
        }

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            _logger.LogWarning("El token para [POST /api/users/change-password] contenía un ID de usuario con formato inválido: {UserIdString}", userIdString);
            return Unauthorized("Token de usuario inválido.");
        }

        var result = await _userService.ChangePasswordAsync(userId, request);

        if (result.Succeeded)
        {
            _logger.LogInformation("Contraseña del usuario {UserId} actualizada exitosamente.", userId);
            return Ok(result);
        }
        _logger.LogWarning("Fallo en la actualización de la contraseña para el usuario {UserId}. Razón: {FailureReason}", userId, result.Message);
        return BadRequest(result);
    }
    
    
}