using CrudCloudDb.Application.Configuration;
using CrudCloudDb.Application.DTOs.Email;
using CrudCloudDb.Application.DTOs.Webhook;
using CrudCloudDb.Application.Interfaces.Repositories;
using CrudCloudDb.Application.Services.Interfaces;
using CrudCloudDb.Core.Entities;
using CrudCloudDb.Core.Enums;
using CrudCloudDb.Infrastructure.Data;
using MercadoPago.Client.MerchantOrder;
using MercadoPago.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;
using System.Linq;

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

        public async Task ProcessMercadoPagoNotificationAsync(MercadoPagoNotification notification)
        {
            _logger.LogInformation("📨 ===== PROCESANDO WEBHOOK DE MERCADOPAGO =====");
            _logger.LogInformation("📋 Tipo: {Type}, Topic: {Topic}, Action: {Action}", 
                notification.Type, notification.Topic, notification.Action ?? "N/A");

            // Crear un nuevo scope para las dependencias scoped
            using var scope = _serviceScopeFactory.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var planRepository = scope.ServiceProvider.GetRequiredService<IPlanRepository>();
            var subscriptionRepository = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Determinar el topic y resource (soportar formato nuevo y legacy)
            var topic = !string.IsNullOrEmpty(notification.Type) ? notification.Type : notification.Topic;
            var resource = !string.IsNullOrEmpty(notification.Data?.Id) 
                ? $"/merchant_orders/{notification.Data.Id}" 
                : notification.Resource;

            _logger.LogInformation("📍 Topic efectivo: {Topic}, Resource: {Resource}", topic, resource);

            // Solo procesamos merchant_order
            if (topic != "merchant_order" && topic != "merchant_orders")
            {
                _logger.LogInformation("ℹ️ Notificación ignorada. Topic '{Topic}' no es 'merchant_order'.", topic);
                return;
            }

            try
            {
                // Extraer el ID de la orden
                var orderIdStr = resource.Split('/').Last();
                var orderId = long.Parse(orderIdStr);

                _logger.LogInformation("🔍 Consultando orden {OrderId} en MercadoPago...", orderId);

                var client = new MerchantOrderClient();
                MercadoPago.Resource.MerchantOrder.MerchantOrder? merchantOrder;

                try
                {
                    merchantOrder = await client.GetAsync(orderId);
                }
                catch (Exception ex) when (ex.Message.Contains("404") || ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("⚠️ Orden {OrderId} no encontrada en MercadoPago (404). " +
                        "Esto puede ocurrir con notificaciones de prueba o pagos antiguos. Ignorando. Error: {Error}", 
                        orderId, ex.Message);
                    return;
                }

                if (merchantOrder == null)
                {
                    _logger.LogWarning("⚠️ Orden {OrderId} retornó null. Ignorando notificación.", orderId);
                    return;
                }

                _logger.LogInformation("📋 Orden {OrderId} obtenida - Status: {Status}, OrderStatus: {OrderStatus}", 
                    orderId, merchantOrder.Status, merchantOrder.OrderStatus);

                var existingSubscription = await subscriptionRepository.FindByOrderIdAsync(orderIdStr);
                if (existingSubscription != null)
                {
                    _logger.LogWarning(
                        "⚠️ La orden de pago {OrderId} ya fue procesada anteriormente. Se ignora la notificación duplicada.",
                        orderId);
                    return;
                }

                if (merchantOrder?.Status == "closed" && merchantOrder.OrderStatus == "paid")
                {
                    _logger.LogInformation("✅ Orden de pago {OrderId} fue aprobada y cerrada. Procesando...", orderId);

                    var externalReference = merchantOrder.ExternalReference;
                    
                    if (string.IsNullOrEmpty(externalReference))
                    {
                        _logger.LogError("❌ Orden {OrderId} no tiene ExternalReference. No se puede procesar.", orderId);
                        return;
                    }

                    _logger.LogInformation("🔑 ExternalReference: {Reference}", externalReference);

                    var parts = externalReference.Split(';');
                    var userIdPart = parts.FirstOrDefault(p => p.StartsWith("user:"))?.Split(':').Last();
                    var planIdPart = parts.FirstOrDefault(p => p.StartsWith("plan:"))?.Split(':').Last();

                    if (Guid.TryParse(userIdPart, out var userId) && Guid.TryParse(planIdPart, out var planId))
                    {
                        _logger.LogInformation("👤 Usuario ID: {UserId}, Plan ID: {PlanId}", userId, planId);

                        var user = await userRepository.GetByIdWithPlanAsync(userId);
                        var plan = await planRepository.GetByIdAsync(planId);

                        if (user != null && plan != null)
                        {
                            var oldPlanName = user.CurrentPlan?.Name ?? "N/A";

                            using (var transaction = await context.Database.BeginTransactionAsync())
                            {
                                try
                                {
                                    user.CurrentPlanId = planId;
                                    await userRepository.UpdateAsync(user);
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
                                        await subscriptionRepository.CreateAsync(newSubscription);

                                        await context.SaveChangesAsync();
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

                            await emailService.SendPlanChangedEmailAsync(new PlanChangedEmailDto
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

                            _logger.LogInformation("🎉 ¡Éxito! El usuario {Email} ahora tiene el plan {PlanName}",
                                user.Email, plan.Name);

                            await SendSuccesNotificationAsync(
                                "✅ Pago Procesado Exitosamente",
                                $"**Usuario:** {user.Email}\n" +
                                $"**Plan Anterior:** {oldPlanName}\n" +
                                $"**Plan Nuevo:** {plan.Name}\n" +
                                $"**Precio:** ${plan.Price} COP\n" +
                                $"**Order ID:** {orderId}\n" +
                                $"**Fecha:** {DateTime.UtcNow.AddHours(-5):dd/MM/yyyy HH:mm:ss} (UTC-5)");
                        }
                        else
                        {
                            _logger.LogError(
                                "❌ No se pudo encontrar el usuario (exists: {UserExists}) o el plan (exists: {PlanExists}) " +
                                "desde la referencia externa: {ExternalReference}",
                                user != null, plan != null, externalReference);

                            await SendWarningNotificationAsync(
                                "⚠️ Pago Recibido pero Usuario/Plan No Encontrado",
                                $"**Order ID:** {orderId}\n" +
                                $"**ExternalReference:** {externalReference}\n" +
                                $"**Usuario encontrado:** {(user != null ? "Sí" : "No")}\n" +
                                $"**Plan encontrado:** {(plan != null ? "Sí" : "No")}\n" +
                                $"**Acción requerida:** Verificar manualmente");
                        }
                    }
                    else
                    {
                        _logger.LogError("❌ No se pudieron parsear los IDs del ExternalReference: {ExternalReference}", externalReference);
                        
                        await SendWarningNotificationAsync(
                            "⚠️ ExternalReference Inválido",
                            $"**Order ID:** {orderId}\n" +
                            $"**ExternalReference:** {externalReference}\n" +
                            $"**Problema:** No se pudieron extraer userId y planId");
                    }
                }
                else if (merchantOrder?.OrderStatus == "rejected")
                {
                    _logger.LogWarning("❌ El pago para la orden {OrderId} fue rechazado.", orderId);
                    
                    // Extraer información del usuario para notificar
                    var externalReference = merchantOrder.ExternalReference;
                    if (!string.IsNullOrEmpty(externalReference))
                    {
                        var parts = externalReference.Split(';');
                        var userIdPart = parts.FirstOrDefault(p => p.StartsWith("user:"))?.Split(':').Last();
                        
                        if (Guid.TryParse(userIdPart, out var userId))
                        {
                            var user = await userRepository.GetByIdAsync(userId);
                            if (user != null)
                            {
                                await SendWarningNotificationAsync(
                                    "❌ Pago Rechazado",
                                    $"**Usuario:** {user.Email}\n" +
                                    $"**Order ID:** {orderId}\n" +
                                    $"**Estado:** Rechazado\n" +
                                    $"**Fecha:** {DateTime.UtcNow.AddHours(-5):dd/MM/yyyy HH:mm:ss} (UTC-5)");
                            }
                        }
                    }
                }
                else
                {
                    // Investigar el estado detallado de los pagos
                    _logger.LogInformation("ℹ️ La orden {OrderId} no está lista para procesar. " +
                        "Status: {Status}, OrderStatus: {OrderStatus}",
                        orderId, merchantOrder?.Status, merchantOrder?.OrderStatus);
                    
                    // Log detallado de los pagos asociados
                    if (merchantOrder?.Payments != null && merchantOrder.Payments.Any())
                    {
                        _logger.LogInformation("💳 La orden tiene {Count} pago(s) asociado(s):", merchantOrder.Payments.Count);
                        
                        foreach (var payment in merchantOrder.Payments)
                        {
                            _logger.LogInformation("  - Payment ID: {PaymentId}, Status: {Status}", 
                                payment.Id, payment.Status);
                        }
                        
                        // Verificar si hay pagos rechazados
                        var rejectedPayments = merchantOrder.Payments.Where(p => p.Status == "rejected").ToList();
                        if (rejectedPayments.Any())
                        {
                            _logger.LogWarning("⚠️ Se encontraron {Count} pago(s) rechazado(s):", rejectedPayments.Count);
                            
                            foreach (var payment in rejectedPayments)
                            {
                                _logger.LogWarning("  ❌ Payment ID: {PaymentId}, Status: rejected", 
                                    payment.Id);
                            }
                            
                            // Enviar notificación sobre pago rechazado
                            var externalReference = merchantOrder.ExternalReference;
                            if (!string.IsNullOrEmpty(externalReference))
                            {
                                var parts = externalReference.Split(';');
                                var userIdPart = parts.FirstOrDefault(p => p.StartsWith("user:"))?.Split(':').Last();
                                
                                if (Guid.TryParse(userIdPart, out var userId))
                                {
                                    var user = await userRepository.GetByIdAsync(userId);
                                    if (user != null)
                                    {
                                        var rejectedDetails = string.Join("\n", rejectedPayments.Select(p => 
                                            $"  - Payment ID: {p.Id}, Status: rejected"));
                                        
                                        await SendWarningNotificationAsync(
                                            "⚠️ Pago(s) Rechazado(s) - Requiere Atención",
                                            $"**Usuario:** {user.Email}\n" +
                                            $"**Order ID:** {orderId}\n" +
                                            $"**Estado Orden:** {merchantOrder.Status} / {merchantOrder.OrderStatus}\n" +
                                            $"**Pagos Rechazados:** {rejectedPayments.Count}\n" +
                                            $"**Detalles:**\n{rejectedDetails}\n" +
                                            $"**Fecha:** {DateTime.UtcNow.AddHours(-5):dd/MM/yyyy HH:mm:ss} (UTC-5)");
                                    }
                                }
                            }
                        }
                        
                        // Verificar si hay pagos pendientes
                        var pendingPayments = merchantOrder.Payments.Where(p => p.Status == "pending" || p.Status == "in_process").ToList();
                        if (pendingPayments.Any())
                        {
                            _logger.LogInformation("⏳ Se encontraron {Count} pago(s) pendiente(s):", pendingPayments.Count);
                            
                            foreach (var payment in pendingPayments)
                            {
                                _logger.LogInformation("  ⏳ Payment ID: {PaymentId}, Status: {Status}", 
                                    payment.Id, payment.Status);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ La orden {OrderId} no tiene pagos asociados aún.", orderId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ Error crítico al procesar la notificación de Mercado Pago para el recurso {Resource}",
                    notification.Resource);
                await SendErrorNotificationAsync(ex, $"Error crítico procesando webhook de Mercado Pago. Recurso: {notification.Resource}");
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