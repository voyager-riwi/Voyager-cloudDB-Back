using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CrudCloudDb.Application.Services.Interfaces;
using CrudCloudDb.Application.DTOs.Database;
using System.Security.Claims;

namespace CrudCloudDb.API.Controllers
{
    /// <summary>
    /// Controller para gestión de bases de datos on-demand
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // ⭐ PROTEGE TODO EL CONTROLLER
    public class DatabasesController : ControllerBase
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<DatabasesController> _logger;
        private readonly IWebhookService _webhookService;

        public DatabasesController(
            IDatabaseService databaseService,
            ILogger<DatabasesController> logger, 
            IWebhookService webhookService)
            
        {
            _databaseService = databaseService;
            _logger = logger;
            _webhookService = webhookService;
        }

        /// <summary>
        /// Obtiene el ID del usuario autenticado desde el JWT
        /// </summary>
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                _logger.LogError("Usuario no autenticado: Falta el claim '{ClaimType}' en el token.", ClaimTypes.NameIdentifier);
                throw new UnauthorizedAccessException("User not authenticated");
            }
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogError("Formato de ID de usuario inválido en el token. Valor del claim: '{UserIdClaim}'.", userIdClaim);
                throw new UnauthorizedAccessException("Invalid user ID format");
            }
            _logger.LogDebug("ID de usuario extraído exitosamente del token: {UserId}.", userId);
            return userId;
        }

        // ============================================
        // POST /api/databases - Crear base de datos
        // ============================================
        /// <summary>
        /// Crea una nueva base de datos para el usuario autenticado
        /// </summary>
        /// <param name="request">Datos de la base de datos (Engine y DatabaseName)</param>
        /// <returns>Información de la base de datos creada con credenciales</returns>
        [HttpPost]
        [ProducesResponseType(typeof(DatabaseResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateDatabase([FromBody] CreateDatabaseRequestDto request)
        {
            try
            {
                _logger.LogInformation("Petición recibida para crear base de datos. Motor: {Engine}.", request.Engine);
                
                var userId = GetCurrentUserId();
                var result = await _databaseService.CreateDatabaseAsync(userId, request);
                
                _logger.LogInformation("Base de datos creada exitosamente. ID: {DatabaseId}.", result.Id);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Acceso no autorizado al intentar crear DB. Mensaje: {ErrorMessage}.", ex.Message);
                return Unauthorized(new { success = false, message = "User not authenticated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error desconocido al intentar crear la base de datos.");
                //return BadRequest(new { success = false, message = ex.Message });
                await _webhookService.SendErrorNotificationAsync(ex,
                    "Error al intentar crear la base de datos (CreateDatabase)");
                return StatusCode(500, new { message = "Ocurrió un error interno en el servidor. El equipo ha sido notificado." });
            }
        }

        // ============================================
        // GET /api/databases - Listar mis bases de datos
        // ============================================
        /// <summary>
        /// Obtiene todas las bases de datos del usuario autenticado
        /// </summary>
        /// <returns>Lista de bases de datos</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<DatabaseResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyDatabases()
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("Iniciando la recuperación de bases de datos para el usuario: {UserId}.", userId);
                
                var databases = await _databaseService.GetUserDatabasesAsync(userId);
                
                _logger.LogInformation("Bases de datos encontradas para el usuario {UserId}: {Count} registros.", userId, databases.Count);
                return Ok(databases);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Acceso no autorizado al intentar obtener bases de datos. Mensaje: {ErrorMessage}.", ex.Message);
                return Unauthorized(new { success = false, message = "User not authenticated" });
            }
        }

        // ============================================
        // GET /api/databases/{id} - Obtener detalles
        // ============================================
        /// <summary>
        /// Obtiene los detalles de una base de datos específica
        /// </summary>
        /// <param name="id">ID de la base de datos</param>
        /// <returns>Detalles de la base de datos</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(DatabaseResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetDatabase(Guid id)
        {
            
            Guid userId = Guid.Empty;
            try     
            {
                userId = GetCurrentUserId();
                _logger.LogInformation("Iniciando búsqueda de base de datos. DB ID: {DatabaseId} | Usuario ID: {UserId}.", id, userId);
                
                var database = await _databaseService.GetDatabaseByIdAsync(userId, id);
                
                if (database == null)
                {
                    _logger.LogWarning("Base de datos no encontrada o usuario sin acceso. DB ID: {DatabaseId}.", id);
                    return NotFound(new { success = false, message = "Database not found" });
                }
                
                _logger.LogInformation("Base de datos encontrada exitosamente. Nombre: {DatabaseName}.", database.Name);
                return Ok(database);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Acceso denegado a la base de datos {DatabaseId} para el usuario {UserId}. Mensaje: {ErrorMessage}.", id, userId, ex.Message);
                return StatusCode(StatusCodes.Status403Forbidden, 
                    new { success = false, message = "You don't have access to this database" });
            }
        }

        // ============================================
        // DELETE /api/databases/{id} - Eliminar base de datos
        // ============================================
        /// <summary>
        /// Elimina una base de datos permanentemente
        /// </summary>
        /// <param name="id">ID de la base de datos</param>
        /// <returns>Confirmación de eliminación</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> DeleteDatabase(Guid id)
        {
            Guid userId = Guid.Empty;
            try
            {
                userId = GetCurrentUserId();
                _logger.LogInformation("Petición recibida para eliminar base de datos. DB ID: {DatabaseId} | Usuario ID: {UserId}.", id, userId);
                
                var success = await _databaseService.DeleteDatabaseAsync(userId, id);
                
                if (!success)
                {
                    _logger.LogWarning("Base de datos no encontrada o usuario no autorizado para eliminar. DB ID: {DatabaseId}.", id);
                    return NotFound(new { success = false, message = "Database not found" });
                }
                
                _logger.LogInformation("Base de datos {DatabaseId} eliminada exitosamente.", id);
                return Ok(new { success = true, message = "Database deleted successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Acceso denegado a la base de datos {DatabaseId} para el usuario {UserId}. Mensaje: {ErrorMessage}.", id, userId, ex.Message);
                return StatusCode(StatusCodes.Status403Forbidden,   
                    new { success = false, message = "You don't have access to this database" });
            }
        }

        // ============================================
        // POST /api/databases/{id}/reset-password
        // ============================================
        /// <summary>
        /// Resetea el password de una base de datos
        /// </summary>
        /// <param name="id">ID de la base de datos</param>
        /// <returns>Confirmación de reset</returns>
        [HttpPost("{id}/reset-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ResetPassword(Guid id)
        {
            Guid userId = Guid.Empty;;
            try
            {
                userId = GetCurrentUserId();
                _logger.LogInformation("Petición de reseteo de contraseña recibida. DB ID: {DatabaseId} | Usuario ID: {UserId}.", id, userId);
                
                await _databaseService.ResetPasswordAsync(userId, id);
                
                _logger.LogInformation("Contraseña reseteada exitosamente para la base de datos {DatabaseId}.", id);
                return Ok(new 
                { 
                    success = true, 
                    message = "Password reset successfully. Check your email for the new password." 
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Acceso denegado al intentar resetear la contraseña de la DB {DatabaseId}. Usuario: {UserId}. Mensaje: {ErrorMessage}.", id, userId, ex.Message);
                return StatusCode(StatusCodes.Status403Forbidden, 
                    new { success = false, message = "You don't have access to this database" });
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning("Base de datos no encontrada (KeyNotFoundException). DB ID: {DatabaseId}.", id);
                return NotFound(new { success = false, message = "Database not found" });
            }
        }

        // ============================================
        // POST /api/databases/{id}/restore
        // ============================================
        /// <summary>
        /// Restaura una base de datos eliminada (dentro del período de gracia de 30 días)
        /// </summary>
        /// <param name="id">ID de la base de datos</param>
        /// <returns>Confirmación de restauración</returns>
        [HttpPost("{id}/restore")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RestoreDatabase(Guid id)
        {
            Guid userId = Guid.Empty;
            try
            {
                userId = GetCurrentUserId();
                _logger.LogInformation("Petición de restauración de base de datos recibida. DB ID: {DatabaseId} | Usuario ID: {UserId}.",
                    id, userId);

                var success = await _databaseService.RestoreDatabaseAsync(userId, id);

                if (!success)
                {
                    _logger.LogWarning("La base de datos no fue restaurada. Razón: no encontrada o ya está activa. DB ID: {DatabaseId}.", id);
                    return NotFound(new { success = false, message = "Database not found or already active" });
                }

                _logger.LogInformation("Base de datos {DatabaseId} restaurada exitosamente.", id);
                return Ok(new { success = true, message = "Database restored successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Acceso denegado al intentar restaurar la DB {DatabaseId}. Usuario: {UserId}. Mensaje: {ErrorMessage}.", id, userId, ex.Message);
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { success = false, message = "You don't have access to this database" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Operación de restauración inválida para la DB {DatabaseId}. Mensaje: {ErrorMessage}.", id, ex.Message);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}