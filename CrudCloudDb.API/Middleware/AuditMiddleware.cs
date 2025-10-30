using System.Diagnostics;

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
            var stopwatch = Stopwatch.StartNew();
            
            var request = context.Request;
            _logger.LogInformation(
                "Request entrante: {Method} {Path}{QueryString}",
                request.Method,
                request.Path,
                request.QueryString.HasValue ? request.QueryString.Value : string.Empty);
            
            await _next(context);

            stopwatch.Stop();
            var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            
            var response = context.Response;
            _logger.LogInformation(
                "Response saliente: {StatusCode} en {ElapsedMilliseconds}ms para {Method} {Path}",
                response.StatusCode,
                elapsedMilliseconds,
                request.Method,
                request.Path);
        }
    }
}