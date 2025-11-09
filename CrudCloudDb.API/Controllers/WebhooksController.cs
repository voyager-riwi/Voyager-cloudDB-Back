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
            _logger.LogInformation("🎯 ===== WEBHOOK RECIBIDO DE MERCADOPAGO =====");
            _logger.LogInformation("📨 Topic: {Topic}", notification.Topic);
            _logger.LogInformation("📨 Resource: {Resource}", notification.Resource);
            _logger.LogInformation("📨 Action: {Action}", notification.Action ?? "N/A");
            _logger.LogInformation("📨 ID: {Id}", notification.Id);
            
            // Log de headers importantes para debugging
            if (Request.Headers.ContainsKey("x-signature"))
            {
                _logger.LogInformation("🔐 X-Signature presente: {Signature}", Request.Headers["x-signature"].ToString());
            }
            if (Request.Headers.ContainsKey("x-request-id"))
            {
                _logger.LogInformation("🔑 X-Request-Id: {RequestId}", Request.Headers["x-request-id"].ToString());
            }
            
            _logger.LogInformation("⚙️ Procesando webhook en segundo plano...");
            
            // Procesar en segundo plano sin esperar (fire and forget)
            _ = Task.Run(() => _webhookService.ProcessMercadoPagoNotificationAsync(notification));
            
            _logger.LogInformation("✅ Webhook aceptado - Respondiendo 200 OK a MercadoPago");
            return Ok();
        }
    }
}