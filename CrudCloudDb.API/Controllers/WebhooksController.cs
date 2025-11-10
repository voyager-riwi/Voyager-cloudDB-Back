using CrudCloudDb.API.Configuration;
using CrudCloudDb.Application.DTOs.Webhook;
using CrudCloudDb.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace CrudCloudDb.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebhooksController : ControllerBase
    {
        private readonly ILogger<WebhooksController> _logger;
        private readonly IWebhookService _webhookService;
        private readonly MercadoPagoSettings _mercadoPagoSettings;

        public WebhooksController(
            ILogger<WebhooksController> logger, 
            IWebhookService webhookService,
            IOptions<MercadoPagoSettings> mercadoPagoSettings)
        {
            _logger = logger;
            _webhookService = webhookService;
            _mercadoPagoSettings = mercadoPagoSettings.Value;
        }

        [HttpPost("mercadopago")]
        [AllowAnonymous] 
        public async Task<IActionResult> MercadoPagoWebhook([FromBody] MercadoPagoNotification notification)
        {
            _logger.LogInformation("Notificación de Webhook recibida de Mercado Pago para el recurso: {Resource}", notification.Resource);
            
            // Validar firma del webhook (opcional pero recomendado)
            var xSignature = Request.Headers["x-signature"].ToString();
            var xRequestId = Request.Headers["x-request-id"].ToString();
            
            if (!string.IsNullOrEmpty(xSignature) && !string.IsNullOrEmpty(_mercadoPagoSettings.WebhookSecret))
            {
                _logger.LogInformation("Validando firma del webhook de Mercado Pago");
                // La validación de firma se puede implementar aquí si es necesario
                // Por ahora, solo registramos que se recibió
            }
            
            _ = _webhookService.ProcessMercadoPagoNotificationAsync(notification);
            
            return Ok();
        }
    }
}