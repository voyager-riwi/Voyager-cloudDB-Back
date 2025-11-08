using CrudCloudDb.Application.Configuration;
using CrudCloudDb.Application.DTOs.Email;
using CrudCloudDb.Application.DTOs.Webhook;
using CrudCloudDb.Application.Interfaces.Repositories;
using CrudCloudDb.Application.Services.Interfaces;
using CrudCloudDb.Core.Entities;
using CrudCloudDb.Core.Enums;
using CrudCloudDb.Infrastructure.Data;
using MercadoPago.Client.MerchantOrder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CrudCloudDb.Infrastructure.Services
{
    public class WebhookService : IWebhookService
    {
        private readonly ILogger<WebhookService> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IPlanRepository _planRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IEmailService _emailService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly WebhookSettings _webhookSettings;
        private readonly ApplicationDbContext _context;

        public WebhookService(
            ILogger<WebhookService> logger,
            IUserRepository userRepository,
            IPlanRepository planRepository,
            ISubscriptionRepository subscriptionRepository,
            IEmailService emailService,
            IHttpClientFactory httpClientFactory,
            IOptions<WebhookSettings> webhookSettings,
            ApplicationDbContext context)
        {
            _logger = logger;
            _userRepository = userRepository;
            _planRepository = planRepository;
            _subscriptionRepository = subscriptionRepository;
            _emailService = emailService;
            _httpClientFactory = httpClientFactory;
            _webhookSettings = webhookSettings.Value;
            _context = context;
        }

        public async Task ProcessMercadoPagoNotificationAsync(MercadoPagoNotification notification)
        {
            _logger.LogInformation("Procesando notificación de Mercado Pago para el recurso: {Resource}",
                notification.Resource);

            if (notification.Topic != "merchant_order")
            {
                _logger.LogInformation("Notificación ignorada. Tópico no es 'merchant_order'.");
                return;
            }

            try
            {
                var orderIdStr = notification.Resource.Split('/').Last();
                var orderId = long.Parse(orderIdStr);

                var client = new MerchantOrderClient();
                var merchantOrder = await client.GetAsync(orderId);

                var existingSubscription = await _subscriptionRepository.FindByOrderIdAsync(orderIdStr);
                if (existingSubscription != null)
                {
                    _logger.LogWarning(
                        "La orden de pago {OrderId} ya fue procesada anteriormente. Se ignora la notificación duplicada.",
                        orderId);
                    return;
                }

                if (merchantOrder?.Status == "closed" && merchantOrder.OrderStatus == "paid")
                {
                    _logger.LogInformation("Orden de pago {OrderId} fue aprobada y cerrada. Procesando...", orderId);

                    var externalReference = merchantOrder.ExternalReference;
                    var parts = externalReference.Split(';');
                    var userIdPart = parts.FirstOrDefault(p => p.StartsWith("user:"))?.Split(':').Last();
                    var planIdPart = parts.FirstOrDefault(p => p.StartsWith("plan:"))?.Split(':').Last();

                    if (Guid.TryParse(userIdPart, out var userId) && Guid.TryParse(planIdPart, out var planId))
                    {
                        var user = await _userRepository.GetByIdWithPlanAsync(userId);
                        var plan = await _planRepository.GetByIdAsync(planId);

                        if (user != null && plan != null)
                        {
                            var oldPlanName = user.CurrentPlan?.Name ?? "N/A";

                            using (var transaction = await _context.Database.BeginTransactionAsync())
                            {
                                try
                                {
                                    user.CurrentPlanId = planId;
                                    await _userRepository.UpdateAsync(user);
                                    _logger.LogInformation(
                                        "Plan del usuario {UserId} actualizado a {PlanId} dentro de la transacción.",
                                        userId, planId);

                                    var payment = merchantOrder.Payments.FirstOrDefault(p => p.Status == "approved");
                                    if (payment != null)
                                    {
                                        var paymentId = payment.Id.ToString();

                                        _logger.LogWarning(
                                            "====== PAYMENT ID PARA VALIDACIÓN DE MERCADO PAGO: {PaymentId} ======",
                                            paymentId);

                                        var newSubscription = new Subscription
                                        {
                                            UserId = userId,
                                            PlanId = planId,
                                            Status = SubscriptionStatus.Active,
                                            StartDate = DateTime.UtcNow,
                                            EndDate = DateTime.UtcNow.AddMonths(1),
                                            MercadoPagoOrderId = merchantOrder.Id.ToString(),
                                            MercadoPagoPaymentId = paymentId
                                        };
                                        await _subscriptionRepository.CreateAsync(newSubscription);

                                        await _context.SaveChangesAsync();
                                        _logger.LogInformation(
                                            "Registro de suscripción creado con Payment ID: {PaymentId} dentro de la transacción.",
                                            paymentId);
                                    }

                                    await transaction.CommitAsync();
                                    _logger.LogInformation(
                                        "Transacción completada exitosamente para la orden {OrderId}.", orderId);
                                }
                                catch (Exception ex)
                                {
                                    await transaction.RollbackAsync();
                                    _logger.LogError(ex,
                                        "Error durante la transacción del webhook para la orden {OrderId}. Se revirtieron los cambios.",
                                        orderId);
                                    throw;
                                }
                            }

                            await _emailService.SendPlanChangedEmailAsync(new PlanChangedEmailDto
                            {
                                UserEmail = user.Email,
                                UserName = user.FirstName,
                                OldPlanName = oldPlanName,
                                NewPlanName = plan.Name,
                                NewPlanPrice = plan.Price,
                                ChangedAt = DateTime.UtcNow,
                                NextBillingDate = DateTime.UtcNow.AddMonths(1),
                                IsRenewal = false
                            });

                            _logger.LogInformation("¡Éxito! El usuario {Email} ahora tiene el plan {PlanName}",
                                user.Email, plan.Name);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "No se pudo encontrar el usuario o el plan desde la referencia externa: {ExternalReference}",
                                externalReference);
                        }
                    }
                }
                else if (merchantOrder?.OrderStatus == "rejected")
                {
                    _logger.LogWarning("El pago para la orden {OrderId} fue rechazado.", orderId);
                }
                else
                {
                    _logger.LogInformation("La orden {OrderId} no está lista para procesar. Estado: {OrderStatus}",
                        orderId, merchantOrder?.OrderStatus);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error crítico al procesar la notificación de Mercado Pago para el recurso {Resource}",
                    notification.Resource);
                await SendErrorNotificationAsync(ex, "Error crítico procesando webhook de Mercado Pago.");
            }
        }

        public async Task SendErrorNotificationAsync(Exception exception, string contextMessage)
        {
            if (string.IsNullOrEmpty(_webhookSettings.DiscordUrl))
            {
                _logger.LogWarning("La URL del webhook de Discord para errores no está configurada.");
                return;
            }

            var httpClient = _httpClientFactory.CreateClient();
            var payload = new
            {
                username = "API Error Bot",
                embeds = new[]
                {
                    new
                    {
                        title = "🚨 Error Inesperado en la API",
                        description = $"**Contexto:** {contextMessage}",
                        color = 15548997,
                        fields = new[]
                        {
                            new { name = "Tipo de Error", value = exception.GetType().Name, inline = true },
                            new { name = "Mensaje", value = exception.Message, inline = false },
                            new
                            {
                                name = "Stack Trace (resumido)",
                                value =
                                    $"```\n{((exception.StackTrace ?? "No stack trace").Length > 1000 ? (exception.StackTrace ?? "No stack trace")[..1000] + "..." : (exception.StackTrace ?? "No stack trace"))}\n```",
                                inline = false
                            }
                        },
                        footer = new { text = $"Timestamp (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}" }
                    }
                }
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync(_webhookSettings.DiscordUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    _logger.LogError(
                        "Falló el envío del webhook a Discord. Código: {StatusCode}, Razón: {Reason}. Respuesta: {ErrorResponse}",
                        response.StatusCode,
                        response.ReasonPhrase,
                        errorResponse);
                }
                else
                {
                    _logger.LogInformation("Webhook de error enviado a Discord con éxito.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocurrió una excepción al intentar enviar el webhook de error a Discord.");
            }
        }


        public async Task SendSuccesNotificationAsync(string title, string message)
        {
            var embed = new
            {
                title = $"{title}",
                description = message,
                color = 3066993,
                footer = new { text = $"TimeStamp: {DateTime.UtcNow.AddHours(-5)}" }
            };

            await SendDiscordWebhookAsync(embed);
        }


        public async Task SendWarningNotificationAsync(string title, string message)
        {
            var embed = new
            {
                title = $"{title}",
                description = message,
                color = 16776960,
                footer = new { text = $"Timestamp: {DateTime.UtcNow.AddHours(-5)}" }
            };

            await SendDiscordWebhookAsync(embed);
        }

        private async Task SendDiscordWebhookAsync(object embedPayload)
        {
            if (string.IsNullOrEmpty(_webhookSettings.DiscordUrl))
            {
                _logger.LogWarning("La url del webhook no ha sido identificada");
                return;
            }

            var payload = new { username = "CrudCloudDb Bot", embeds = new[] { embedPayload } };
            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.PostAsync(_webhookSettings.DiscordUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Falló el envio del webhook a Discord, Codigo de estado {statuscode}",
                        response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocurrió una excepción al intentar enviar el webhook a Discord.");
            }

        }
    }
}