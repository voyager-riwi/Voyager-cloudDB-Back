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
using Microsoft.Extensions.DependencyInjection;
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
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly WebhookSettings _webhookSettings;

        public WebhookService(
            ILogger<WebhookService> logger,
            IServiceScopeFactory serviceScopeFactory,
            IHttpClientFactory httpClientFactory,
            IOptions<WebhookSettings> webhookSettings)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _httpClientFactory = httpClientFactory;
            _webhookSettings = webhookSettings.Value;
        }

        public async Task ProcessMercadoPagoNotificationAsync(MercadoPagoNotification notification, string? signatureHeader, string? requestId)
        {
            _logger.LogInformation("🔔 Webhook recibido de MercadoPago (RequestId: {RequestId})", requestId ?? "N/A");
            _logger.LogInformation("   📋 Topic: {Topic}", notification.Topic);
            _logger.LogInformation("   🔗 Resource: {Resource}", string.IsNullOrWhiteSpace(notification.Resource) ? "N/A" : notification.Resource);
            _logger.LogInformation("   🆔 Data.Id: {DataId}", notification.Data?.Id ?? "N/A");
            _logger.LogInformation("   🆔 Action: {Action}", notification.Action ?? "N/A");

            if (!IsSignatureValid(signatureHeader, notification, requestId))
            {
                _logger.LogWarning("Firma ausente o no válida. Ignorando webhook (RequestId: {RequestId}).", requestId ?? "N/A");
                return;
            }

            var entityId = ExtractNotificationEntityId(notification);
            if (string.IsNullOrEmpty(entityId))
            {
                _logger.LogWarning("No se pudo determinar el identificador de la entidad desde la notificación.");
                return;
            }

            if (!long.TryParse(entityId, out var numericId))
            {
                _logger.LogWarning("El identificador '{EntityId}' no es numérico. RequestId: {RequestId}", entityId, requestId ?? "N/A");
                return;
            }

            using var scope = _serviceScopeFactory.CreateScope();
            var resources = new ProcessingResources(
                scope.ServiceProvider.GetRequiredService<IUserRepository>(),
                scope.ServiceProvider.GetRequiredService<IPlanRepository>(),
                scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>(),
                scope.ServiceProvider.GetRequiredService<IEmailService>(),
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());

            try
            {
                // Determinar el topic desde notification.Topic o desde notification.Action
                var topic = notification.Topic;
                if (string.IsNullOrWhiteSpace(topic) && !string.IsNullOrWhiteSpace(notification.Action))
                {
                    // Extraer el topic desde Action (ej: "payment.updated" -> "payment")
                    topic = notification.Action.Split('.').FirstOrDefault() ?? string.Empty;
                    _logger.LogInformation("📌 Topic extraído desde Action: {Topic}", topic);
                }

                switch (topic)
                {
                    case "payment":
                        await HandlePaymentAsync(numericId, requestId, null, resources);
                        break;
                    case "merchant_order":
                        await HandleMerchantOrderAsync(numericId, requestId, resources);
                        break;
                    default:
                        _logger.LogWarning("⚠️ Tópico '{Topic}' no soportado actualmente.", topic);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico durante el procesamiento del webhook (RequestId: {RequestId}).", requestId ?? "N/A");
                await SendErrorNotificationAsync(ex, "Error crítico procesando webhook de Mercado Pago.");
            }
        }

        private bool IsSignatureValid(string? signatureHeader, MercadoPagoNotification notification, string? requestId)
        {
            if (string.IsNullOrWhiteSpace(_webhookSettings.SignatureSecret))
            {
                _logger.LogDebug("Secret de webhook no configurado. Se omite validación de firma.");
                return true;
            }

            if (string.IsNullOrWhiteSpace(signatureHeader))
            {
                _logger.LogWarning("No se recibió la cabecera x-signature en el webhook (RequestId: {RequestId}).", requestId ?? "N/A");
                return false;
            }

            try
            {
                var values = signatureHeader
                    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(part => part.Split('=', 2, StringSplitOptions.TrimEntries))
                    .Where(kv => kv.Length == 2)
                    .ToDictionary(kv => kv[0].Trim().ToLowerInvariant(), kv => kv[1].Trim(), StringComparer.OrdinalIgnoreCase);

                if (values.TryGetValue("id", out var idFromSignature))
                {
                    var notificationId = ExtractNotificationEntityId(notification);
                    if (!string.IsNullOrEmpty(notificationId) && !string.Equals(notificationId, idFromSignature, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("El id de la firma ({SignatureId}) no coincide con el id de la notificación ({NotificationId}).", idFromSignature, notificationId);
                        return false;
                    }
                }

                if (values.TryGetValue("sha256", out var expectedHash) && values.TryGetValue("ts", out var timestamp))
                {
                    var baseString = $"id:{values.GetValueOrDefault("id", string.Empty)};topic:{notification.Topic};ts:{timestamp}";
                    using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(_webhookSettings.SignatureSecret));
                    var computedHash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(baseString))).ToLowerInvariant();

                    if (!string.Equals(computedHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("La firma calculada no coincide con la recibida (RequestId: {RequestId}). Se continúa para no perder el evento, pero revisa la configuración del secret.", requestId ?? "N/A");
                    }
                }
                else
                {
                    _logger.LogWarning("El formato de la cabecera x-signature no contiene sha256/ts. Header: {Signature}", signatureHeader);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al intentar validar la firma del webhook (RequestId: {RequestId}).", requestId ?? "N/A");
            }

            return true;
        }

        private static string? ExtractNotificationEntityId(MercadoPagoNotification notification)
        {
            if (!string.IsNullOrWhiteSpace(notification.Data?.Id))
            {
                return notification.Data.Id;
            }

            if (notification.Id.HasValue)
            {
                return notification.Id.Value.ToString();
            }

            if (!string.IsNullOrWhiteSpace(notification.Resource))
            {
                var segments = notification.Resource
                    .TrimEnd('/')
                    .Split('/', StringSplitOptions.RemoveEmptyEntries);

                return segments.LastOrDefault();
            }

            return null;
        }

        private async Task HandleMerchantOrderAsync(long orderId, string? requestId, ProcessingResources resources)
        {
            _logger.LogInformation("📦 Procesando merchant_order {OrderId} (RequestId: {RequestId})", orderId, requestId ?? "N/A");

            var client = new MerchantOrderClient();
            MercadoPago.Resource.MerchantOrder.MerchantOrder? merchantOrder;

            try
            {
                merchantOrder = await client.GetAsync(orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar merchant_order {OrderId}.", orderId);
                await SendErrorNotificationAsync(ex, $"Error consultando merchant_order {orderId}.");
                return;
            }

            if (merchantOrder == null)
            {
                _logger.LogWarning("No se encontró la orden {OrderId} en Mercado Pago.", orderId);
                return;
            }

            _logger.LogInformation("   🔍 Status: {Status}", merchantOrder.Status ?? "NULL");
            _logger.LogInformation("   💰 OrderStatus: {OrderStatus}", merchantOrder.OrderStatus ?? "NULL");
            _logger.LogInformation("   💳 Pagos totales: {Count}", merchantOrder.Payments?.Count ?? 0);

            var approvedPayment = merchantOrder.Payments?
                .FirstOrDefault(p => string.Equals(p.Status, "approved", StringComparison.OrdinalIgnoreCase));

            if (approvedPayment?.Id == null)
            {
                _logger.LogInformation("La orden {OrderId} no tiene pagos aprobados todavía.", orderId);
                return;
            }

            await HandlePaymentAsync(
                approvedPayment.Id.Value,
                requestId,
                merchantOrder.Id?.ToString(),
                resources);
        }

        private async Task HandlePaymentAsync(long paymentId, string? requestId, string? fallbackMerchantOrderId, ProcessingResources resources)
        {
            _logger.LogInformation("💳 Procesando pago {PaymentId} (RequestId: {RequestId})", paymentId, requestId ?? "N/A");

            var paymentClient = new MercadoPago.Client.Payment.PaymentClient();
            MercadoPago.Resource.Payment.Payment? payment;

            try
            {
                payment = await paymentClient.GetAsync(paymentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar el pago {PaymentId} en Mercado Pago.", paymentId);
                await SendErrorNotificationAsync(ex, $"Error consultando pago {paymentId}.");
                return;
            }

            if (payment == null)
            {
                _logger.LogWarning("El pago {PaymentId} no fue encontrado en Mercado Pago.", paymentId);
                return;
            }

            var status = payment.Status ?? "N/A";
            if (!string.Equals(status, "approved", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("El pago {PaymentId} tiene estado {Status}. Se esperará a que sea 'approved'.", paymentId, status);
                return;
            }

            var paymentIdStr = payment.Id?.ToString() ?? paymentId.ToString();

            var existingByPayment = await resources.SubscriptionRepository.FindByPaymentIdAsync(paymentIdStr);
            if (existingByPayment != null)
            {
                _logger.LogWarning("El pago {PaymentId} ya fue procesado previamente (SubscriptionId: {SubscriptionId}).", paymentIdStr, existingByPayment.Id);
                return;
            }

            var merchantOrderId = payment.Order?.Id?.ToString() ?? fallbackMerchantOrderId;
            if (!string.IsNullOrEmpty(merchantOrderId))
            {
                var existingByOrder = await resources.SubscriptionRepository.FindByOrderIdAsync(merchantOrderId);
                if (existingByOrder != null)
                {
                    _logger.LogWarning("La orden {OrderId} asociada al pago {PaymentId} ya fue procesada (SubscriptionId: {SubscriptionId}).", merchantOrderId, paymentIdStr, existingByOrder.Id);
                    return;
                }
            }

            var externalReference = payment.ExternalReference;
            if (string.IsNullOrWhiteSpace(externalReference))
            {
                _logger.LogWarning("El pago {PaymentId} no tiene external_reference. No se puede identificar usuario/plan.", paymentIdStr);
                return;
            }

            var (userId, planId) = ParseExternalReference(externalReference);
            if (userId == null || planId == null)
            {
                _logger.LogWarning("No se pudo extraer userId/planId de external_reference '{ExternalReference}'.", externalReference);
                return;
            }

            var user = await resources.UserRepository.GetByIdWithPlanAsync(userId.Value);
            if (user == null)
            {
                _logger.LogWarning("Usuario {UserId} no encontrado al procesar pago {PaymentId}.", userId, paymentIdStr);
                return;
            }

            var plan = await resources.PlanRepository.GetByIdAsync(planId.Value);
            if (plan == null)
            {
                _logger.LogWarning("Plan {PlanId} no encontrado al procesar pago {PaymentId}.", planId, paymentIdStr);
                return;
            }

            if (!string.IsNullOrEmpty(payment.CurrencyId) && !string.Equals(payment.CurrencyId, "COP", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("La moneda del pago {PaymentId} ({Currency}) no coincide con la esperada (COP).", paymentIdStr, payment.CurrencyId);
            }

            var paidAmount = (decimal?)payment.TransactionAmount ?? 0m;
            if (paidAmount < plan.Price)
            {
                _logger.LogWarning("El monto pagado {PaidAmount} es menor al precio del plan {PlanPrice}.", paidAmount, plan.Price);
            }

            var previousPlanName = user.CurrentPlan?.Name ?? "N/A";

            using var transaction = await resources.DbContext.Database.BeginTransactionAsync();
            try
            {
                user.CurrentPlanId = plan.Id;
                await resources.UserRepository.UpdateAsync(user);

                var newSubscription = new Subscription
                {
                    UserId = user.Id,
                    PlanId = plan.Id,
                    Status = SubscriptionStatus.Active,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddMonths(1),
                    MercadoPagoOrderId = merchantOrderId,
                    MercadoPagoPaymentId = paymentIdStr
                };

                await resources.SubscriptionRepository.CreateAsync(newSubscription);

                await transaction.CommitAsync();

                _logger.LogInformation("Suscripción {SubscriptionId} creada/actualizada para el usuario {UserId} con el plan {PlanName}.", newSubscription.Id, user.Id, plan.Name);

                try
                {
                    await resources.EmailService.SendPlanChangedEmailAsync(new PlanChangedEmailDto
                    {
                        UserEmail = user.Email,
                        UserName = user.FirstName,
                        OldPlanName = previousPlanName,
                        NewPlanName = plan.Name,
                        NewPlanPrice = plan.Price,
                        ChangedAt = DateTime.UtcNow,
                        NextBillingDate = DateTime.UtcNow.AddMonths(1),
                        IsRenewal = false
                    });
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Error al enviar el correo de cambio de plan para el usuario {UserId}.", user.Id);
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error durante la transacción del pago {PaymentId}.", paymentId);
                throw;
            }
        }

        private static (Guid? userId, Guid? planId) ParseExternalReference(string externalReference)
        {
            Guid? userId = null;
            Guid? planId = null;

            var segments = externalReference.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var segment in segments)
            {
                var parts = segment.Split(':', 2, StringSplitOptions.TrimEntries);
                if (parts.Length != 2)
                {
                    continue;
                }

                if (parts[0].Equals("user", StringComparison.OrdinalIgnoreCase) && Guid.TryParse(parts[1], out var parsedUser))
                {
                    userId = parsedUser;
                }
                else if (parts[0].Equals("plan", StringComparison.OrdinalIgnoreCase) && Guid.TryParse(parts[1], out var parsedPlan))
                {
                    planId = parsedPlan;
                }
            }

            return (userId, planId);
        }

        private sealed record ProcessingResources(
            IUserRepository UserRepository,
            IPlanRepository PlanRepository,
            ISubscriptionRepository SubscriptionRepository,
            IEmailService EmailService,
            ApplicationDbContext DbContext);

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