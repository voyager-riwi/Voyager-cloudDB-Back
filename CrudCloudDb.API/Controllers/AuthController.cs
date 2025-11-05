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
            _logger.LogWarning("Fallo la validacion del modelo para el registro");
            return BadRequest(ModelState);
        }
        _logger.LogInformation("Petición de registro recibida para el correo: {Email}.", request.Email);
        var result = await _authService.RegisterAsync(request);

        if (result.Succeeded)
        {
            _logger.LogInformation("Registro exitoso para el usuario: {Email}. User ID: {UserId}.", request.Email, result.Data.UserId);
            return Ok(result);
        }
        _logger.LogWarning("Fallo en el registro para el correo: {Email}. Errores: {Errors}", request.Email, result.Errors);
        return BadRequest(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Fallo en la validación del modelo para el inicio de sesión.");
            return BadRequest(request);
        }
        _logger.LogDebug("Petición de inicio de sesión recibida para el correo: {Email}.", request.Email);
        var result = await _authService.LoginAsync(request);
        
        if (result.Succeeded)
        {
            _logger.LogInformation("Inicio de sesión exitoso para el usuario: {Email}. User ID: {UserId}.", 
                request.Email, result.Data.UserId);
            return Ok(result);
        }
        _logger.LogWarning("Fallo en el intento de inicio de sesión para el correo: {Email}.", request.Email);
        return Unauthorized(result);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Fallo en la validación del modelo para el cambio de contraseña.");
            return BadRequest();
        }
        
        var result = await _authService.ForgotPasswordAsync(request);
        _logger.LogDebug("Proceso de recuperación de contraseña finalizado para {Email}. Resultado: {ResultSucceeded}.", request.Email, result.Succeeded);
        return Ok(result);
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(request);
        }
        
        var result = await _authService.ResetPasswordAsync(request);
        
        if (result.Succeeded)
        {
            _logger.LogInformation("Reseteo de contraseña exitoso para el correo: {Email}.", result.Message);
            return Ok(result);
        }
        _logger.LogWarning("Fallo en el reseteo de contraseña");
        return BadRequest(result);
    }
}