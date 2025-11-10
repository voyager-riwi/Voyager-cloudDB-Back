using CrudCloudDb.API.Configuration;
using CrudCloudDb.Application.DTOs.Payment;
using CrudCloudDb.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace CrudCloudDb.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] 
    public class PaymentsController : ControllerBase
    {
        private readonly ILogger<PaymentsController> _logger;
        private readonly IPaymentService _paymentService;
        private readonly IWebhookService _webhookService;
        private readonly MercadoPagoSettings _mercadoPagoSettings;

        public PaymentsController(
            ILogger<PaymentsController> logger, 
            IPaymentService paymentService, 
            IWebhookService webhookService,
            IOptions<MercadoPagoSettings> mercadoPagoSettings)
        {
            _logger = logger;
            _paymentService = paymentService;
            _webhookService = webhookService;
            _mercadoPagoSettings = mercadoPagoSettings.Value;
        }

        /// <summary>
        /// Obtiene la Public Key de Mercado Pago para el frontend
        /// </summary>
        [HttpGet("public-key")]
        [AllowAnonymous]
        public IActionResult GetPublicKey()
        {
            _logger.LogInformation("Solicitada Public Key de Mercado Pago");
            
            if (string.IsNullOrEmpty(_mercadoPagoSettings.PublicKey))
            {
                _logger.LogWarning("Public Key de Mercado Pago no configurada");
                return StatusCode(500, new { message = "Mercado Pago no está configurado correctamente" });
            }

            return Ok(new { publicKey = _mercadoPagoSettings.PublicKey });
        }

        [HttpPost("create-preference")]
        public async Task<IActionResult> CreatePreference([FromBody] CreatePreferenceRequestDto request)
        {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(userIdString, out var userId))
                {
                    return Unauthorized("Token de usuario inválido.");
                }
                _logger.LogInformation("Recibida solicitud para crear preferencia de pago por usuario {UserId}", userId);

                var result = await _paymentService.CreateSubscriptionPreferenceAsync(userId, request);

                if (result.Succeeded)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
        }
    }
