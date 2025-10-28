using CrudCloudDb.Application.Interfaces.Repositories; // Se usará en el futuro
using CrudCloudDb.Core.Entities; // Se usará en el futuro
using System.Security.Claims;

namespace CrudCloudDb.API.Middleware
{
    public class AuditMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuditMiddleware> _logger;

        public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var startTime = DateTime.UtcNow;

            await _next(context); // Dejar que la petición continúe hasta el controlador

            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            var request = context.Request;
            var response = context.Response;

            // Obtener el ID del usuario desde los claims del token JWT (si está autenticado)
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            var userId = userIdClaim != null ? userIdClaim.Value : "Anónimo";

            var logMessage = $"AUDIT LOG: " +
                             $"Path: {request.Path}, " +
                             $"Method: {request.Method}, " +
                             $"StatusCode: {response.StatusCode}, " +
                             $"UserId: {userId}, " +
                             $"Duration: {duration.TotalMilliseconds:F2} ms";

            // Usamos el logger como placeholder por ahora
            _logger.LogInformation(logMessage);

            // --- Lógica para guardar en Base de Datos (FUTURO) ---
            // TODO: Cuando Miguel implemente IAuditLogRepository,
            // inyectaremos el servicio aquí y guardaremos el registro.
            //
            // var auditLog = new AuditLog
            // {
            //     UserId = Guid.TryParse(userId, out var id) ? id : null,
            //     Action = $"{request.Method} {request.Path}",
            //     Timestamp = startTime,
            //     // ... otros campos
            // };
            //
            // await auditLogRepository.CreateAsync(auditLog);
        }
    }
}