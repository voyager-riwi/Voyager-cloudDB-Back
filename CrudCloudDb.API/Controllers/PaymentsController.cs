using CrudCloudDb.Application.DTOs.Payment;
using CrudCloudDb.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public PaymentsController(ILogger<PaymentsController> logger, IPaymentService paymentService, IWebhookService webhookService)
        {
            _logger = logger;
            _paymentService = paymentService;
            _webhookService = webhookService;
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
