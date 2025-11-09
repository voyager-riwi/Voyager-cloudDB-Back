﻿using CrudCloudDb.Application.DTOs.Webhook;
using CrudCloudDb.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        public IActionResult MercadoPagoWebhook([FromBody] MercadoPagoNotification notification)
        {
            _logger.LogInformation("Notificación de Webhook recibida de Mercado Pago para el recurso: {Resource}", notification.Resource);
            
            // Procesar en segundo plano sin esperar (fire and forget)
            _ = Task.Run(() => _webhookService.ProcessMercadoPagoNotificationAsync(notification));
            
            return Ok();
        }
    }
}