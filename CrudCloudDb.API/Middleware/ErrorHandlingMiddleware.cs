// --- VERSIÓN CORREGIDA ---
using System.Net;
using System.Text.Json;
using CrudCloudDb.Application.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CrudCloudDb.API.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;
        
        public ErrorHandlingMiddleware(
            RequestDelegate next, 
            ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        
        public async Task InvokeAsync(HttpContext context, IWebhookService webhookService)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ha ocurrido una excepción no controlada.");
                
                await webhookService.SendErrorNotificationAsync(ex, "Error global no controlado en la API.");

                var response = context.Response;
                response.ContentType = "application/json";
                response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var errorResponse = new
                {
                    message = "Ocurrió un error interno en el servidor. El equipo ha sido notificado.",
                    statusCode = response.StatusCode
                };

                var jsonResponse = JsonSerializer.Serialize(errorResponse);
                await response.WriteAsync(jsonResponse);
            }
        }
    }
}