using CrudCloudDb.Application.DTOs.Webhook;
using CrudCloudDb.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace CrudCloudDb.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebhooksController : ControllerBase
    {
        private readonly ILogger<WebhooksController> _logger;
        private readonly IWebhookService _webhookService;

        public WebhooksController(ILogger<WebhooksController> logger, IWebhookService webhookService)
        {
            _logger = logger;
            _webhookService = webhookService;
        }

        [HttpPost("mercadopago")]
        [AllowAnonymous] 
        public async Task<IActionResult> MercadoPagoWebhook([FromBody] MercadoPagoNotification notification)
        {
            _logger.LogInformation("Notificación de Webhook recibida de Mercado Pago para el recurso: {Resource}", notification.Resource);

            var signature = Request.Headers["x-signature"].FirstOrDefault();
            var requestId = Request.Headers["x-request-id"].FirstOrDefault();

            await _webhookService.ProcessMercadoPagoNotificationAsync(notification, signature, requestId);

            return Ok();
        }
    }
}