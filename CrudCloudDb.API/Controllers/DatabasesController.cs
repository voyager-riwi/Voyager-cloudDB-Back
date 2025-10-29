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
    // [Authorize] // ← Descomentar cuando Auth esté listo
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
            // TODO: Cuando Auth esté listo, usar esto:
            // var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            // if (string.IsNullOrEmpty(userIdClaim))
            //     throw new UnauthorizedAccessException("User not authenticated");
            // return Guid.Parse(userIdClaim);

            // ⚠️ TEMPORAL: Usuario hardcodeado para testing
            return Guid.Parse("11111111-1111-1111-1111-111111111111");
        }

        // ============================================
        // POST /api/databases
        // ============================================
        /// <summary>
        /// Crea una nueva base de datos
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(DatabaseResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateDatabase([FromBody] CreateDatabaseRequestDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _databaseService.CreateDatabaseAsync(userId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating database");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ============================================
        // GET /api/databases
        // ============================================
        /// <summary>
        /// Obtiene todas las bases de datos del usuario
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<DatabaseResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyDatabases()
        {
            try
            {
                var userId = GetCurrentUserId();
                var databases = await _databaseService.GetUserDatabasesAsync(userId);
                return Ok(databases);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting databases");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ============================================
        // GET /api/databases/{id}
        // ============================================
        /// <summary>
        /// Obtiene detalles de una base de datos específica
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(DatabaseResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetDatabase(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var database = await _databaseService.GetDatabaseByIdAsync(userId, id);
                
                if (database == null)
                    return NotFound(new { success = false, message = "Database not found" });
                
                return Ok(database);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting database");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ============================================
        // DELETE /api/databases/{id}
        // ============================================
        /// <summary>
        /// Elimina una base de datos
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteDatabase(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _databaseService.DeleteDatabaseAsync(userId, id);
                
                if (!success)
                    return NotFound(new { success = false, message = "Database not found" });
                
                return Ok(new { success = true, message = "Database deleted successfully" });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting database");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ============================================
        // POST /api/databases/{id}/reset-password
        // ============================================
        /// <summary>
        /// Resetea el password de una base de datos
        /// </summary>
        [HttpPost("{id}/reset-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ResetPassword(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _databaseService.ResetPasswordAsync(userId, id);
                return Ok(new { success = true, message = "Password reset successfully. Check your email." });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ============================================
        // POST /api/databases/{id}/start
        // ============================================
        /// <summary>
        /// Inicia un contenedor detenido
        /// </summary>
        [HttpPost("{id}/start")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> StartDatabase(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _databaseService.StartDatabaseAsync(userId, id);
                return Ok(new { success, message = success ? "Database started" : "Failed to start" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting database");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ============================================
        // POST /api/databases/{id}/stop
        // ============================================
        /// <summary>
        /// Detiene un contenedor
        /// </summary>
        [HttpPost("{id}/stop")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> StopDatabase(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _databaseService.StopDatabaseAsync(userId, id);
                return Ok(new { success, message = success ? "Database stopped" : "Failed to stop" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping database");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}