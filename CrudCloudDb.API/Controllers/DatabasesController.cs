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

        public DatabasesController(
            IDatabaseService databaseService,
            ILogger<DatabasesController> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene el ID del usuario autenticado desde el JWT
        /// </summary>
        private Guid GetCurrentUserId()
        {
            // ⭐ LEE EL USER ID DEL TOKEN JWT
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim))
            {
                _logger.LogError("❌ User not authenticated - no NameIdentifier claim found");
                throw new UnauthorizedAccessException("User not authenticated");
            }

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogError($"❌ Invalid user ID format in token: {userIdClaim}");
                throw new UnauthorizedAccessException("Invalid user ID format");
            }

            _logger.LogInformation($"✅ User authenticated: {userId}");
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
                _logger.LogInformation($"📥 Create database request: {request.Engine}");
                
                var userId = GetCurrentUserId();
                var result = await _databaseService.CreateDatabaseAsync(userId, request);
                
                _logger.LogInformation($"✅ Database created successfully: {result.Id}");
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning($"⚠️ Unauthorized: {ex.Message}");
                return Unauthorized(new { success = false, message = "User not authenticated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating database");
                return BadRequest(new { success = false, message = ex.Message });
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
                _logger.LogInformation($"📋 Getting databases for user: {userId}");
                
                var databases = await _databaseService.GetUserDatabasesAsync(userId);
                
                _logger.LogInformation($"✅ Found {databases.Count} databases");
                return Ok(databases);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning($"⚠️ Unauthorized: {ex.Message}");
                return Unauthorized(new { success = false, message = "User not authenticated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting databases");
                return BadRequest(new { success = false, message = ex.Message });
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
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation($"🔍 Getting database {id} for user {userId}");
                
                var database = await _databaseService.GetDatabaseByIdAsync(userId, id);
                
                if (database == null)
                {
                    _logger.LogWarning($"⚠️ Database {id} not found");
                    return NotFound(new { success = false, message = "Database not found" });
                }
                
                _logger.LogInformation($"✅ Database found: {database.Name}");
                return Ok(database);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning($"⚠️ Access denied: {ex.Message}");
                return StatusCode(StatusCodes.Status403Forbidden, 
                    new { success = false, message = "You don't have access to this database" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error getting database {id}");
                return BadRequest(new { success = false, message = ex.Message });
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
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation($"🗑️ Delete database request: {id} by user {userId}");
                
                var success = await _databaseService.DeleteDatabaseAsync(userId, id);
                
                if (!success)
                {
                    _logger.LogWarning($"⚠️ Database {id} not found");
                    return NotFound(new { success = false, message = "Database not found" });
                }
                
                _logger.LogInformation($"✅ Database {id} deleted successfully");
                return Ok(new { success = true, message = "Database deleted successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning($"⚠️ Access denied: {ex.Message}");
                return StatusCode(StatusCodes.Status403Forbidden, 
                    new { success = false, message = "You don't have access to this database" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error deleting database {id}");
                return BadRequest(new { success = false, message = ex.Message });
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
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation($"🔑 Reset password request for database {id}");
                
                await _databaseService.ResetPasswordAsync(userId, id);
                
                _logger.LogInformation($"✅ Password reset successfully for database {id}");
                return Ok(new 
                { 
                    success = true, 
                    message = "Password reset successfully. Check your email for the new password." 
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning($"⚠️ Access denied: {ex.Message}");
                return StatusCode(StatusCodes.Status403Forbidden, 
                    new { success = false, message = "You don't have access to this database" });
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning($"⚠️ Database {id} not found");
                return NotFound(new { success = false, message = "Database not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error resetting password for database {id}");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ============================================
        // POST /api/databases/{id}/start
        // ============================================
        /// <summary>
        /// Inicia un contenedor maestro detenido
        /// </summary>
        /// <param name="id">ID de la base de datos</param>
        /// <returns>Confirmación de inicio</returns>
        [HttpPost("{id}/start")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> StartDatabase(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation($"▶️ Start database request: {id}");
                
                var success = await _databaseService.StartDatabaseAsync(userId, id);
                
                var message = success 
                    ? "Database started successfully" 
                    : "Failed to start database";
                
                _logger.LogInformation($"{(success ? "✅" : "❌")} {message}");
                
                return Ok(new { success, message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error starting database {id}");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ============================================
        // POST /api/databases/{id}/stop
        // ============================================
        /// <summary>
        /// Detiene un contenedor maestro
        /// </summary>
        /// <param name="id">ID de la base de datos</param>
        /// <returns>Confirmación de detención</returns>
        [HttpPost("{id}/stop")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> StopDatabase(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation($"⏸️ Stop database request: {id}");
                
                var success = await _databaseService.StopDatabaseAsync(userId, id);
                
                var message = success 
                    ? "Database stopped successfully" 
                    : "Failed to stop database";
                
                _logger.LogInformation($"{(success ? "✅" : "❌")} {message}");
                
                return Ok(new { success, message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error stopping database {id}");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}