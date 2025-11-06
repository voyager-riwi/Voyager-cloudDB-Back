using CrudCloudDb.Application.Configuration;
using CrudCloudDb.Application.DTOs.Webhook;
using CrudCloudDb.Application.Interfaces.Repositories;
using CrudCloudDb.Application.Services.Interfaces;
using CrudCloudDb.Core.Enums;
using MercadoPago.Client.MerchantOrder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CrudCloudDb.Infrastructure.Services;

public class WebhookService : IWebhookService
{
    private readonly ILogger<WebhookService> _logger;
    private readonly IUserRepository _userRepository;
    private readonly IPlanRepository _planRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly Application.Interfaces.Repositories.IDatabaseInstanceRepository _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly WebhookSettings _webhookSettings;

    public WebhookService(
        ILogger<WebhookService> logger,
        IUserRepository userRepository,
        IPlanRepository planRepository,
        ISubscriptionRepository subscriptionRepository,
        Application.Interfaces.Repositories.IDatabaseInstanceRepository db,
        IHttpClientFactory httpClientFactory,
        IOptions<WebhookSettings> webhookSettings)
    {
        _logger = logger;
        _userRepository = userRepository;
        _planRepository = planRepository;
        _subscriptionRepository = subscriptionRepository;
        _db = db;
        _httpClientFactory = httpClientFactory;
        _webhookSettings = webhookSettings.Value;
    }

    public async Task ProcessMercadoPagoNotificationAsync(MercadoPagoNotification notification)
    {
        _logger.LogInformation("Procesando notificación de Mercado Pago para el recurso: {Resource}", notification.Resource);

        if (notification.Topic != "merchant_order")
        {
            _logger.LogInformation("Notificación ignorada. Tópico no es 'merchant_order'.");
            return;
        }

        try
        {
            var orderId = long.Parse(notification.Resource.Split('/').Last());
            var client = new MerchantOrderClient();
            var merchantOrder = await client.GetAsync(orderId);

            if (merchantOrder?.Status == "closed" && merchantOrder.OrderStatus == "paid")
            {
                _logger.LogInformation("Orden de pago {OrderId} fue aprobada y cerrada. Procesando...", orderId);

                var externalReference = merchantOrder.ExternalReference;
                var parts = externalReference.Split(';');
                var userIdPart = parts.FirstOrDefault(p => p.StartsWith("user:"))?.Split(':').Last();
                var planIdPart = parts.FirstOrDefault(p => p.StartsWith("plan:"))?.Split(':').Last();

                if (Guid.TryParse(userIdPart, out var userId) && Guid.TryParse(planIdPart, out var planId))
                {
                    var user = await _userRepository.GetByIdAsync(userId);
                    var plan = await _planRepository.GetByIdAsync(planId);

                    if (user != null && plan != null)
                    {
                        _logger.LogInformation("Actualizando plan para el usuario {UserId} al plan {PlanId}", userId, planId);
                        user.CurrentPlanId = planId;
                        await _userRepository.UpdateAsync(user);
                        
                        _logger.LogInformation("¡Éxito! El usuario {Email} ahora tiene el plan {PlanName}", user.Email, plan.Name);
                    }
                    else
                    {
                        _logger.LogWarning("No se pudo encontrar el usuario o el plan desde la referencia externa: {ExternalReference}", externalReference);
                    }
                }
                else
                {
                    _logger.LogWarning("La referencia externa no tiene el formato esperado: {ExternalReference}", externalReference);
                }
            }
            else
            {
                _logger.LogInformation("La orden {OrderId} no está lista para procesar. Estado: {OrderStatus}", orderId, merchantOrder?.OrderStatus);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar la notificación de Mercado Pago para el recurso {Resource}", notification.Resource);
        }
    }

    public async Task SendErrorNotificationAsync(Exception exception, string contextMessage)
    {
        Console.WriteLine("Url de discord: " + _webhookSettings.DiscordUrl);
        
        if (string.IsNullOrEmpty(_webhookSettings.DiscordUrl))
        {
            Console.WriteLine(_webhookSettings.DiscordUrl);
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
                    title = "Error inesperado en la API",
                    description = $"**Contexto:** {contextMessage}",
                    color = 15548997, 
                    fields = new[] 
                    {
                        new { name = "Tipo de Error", value = exception.GetType().Name, inline = true },
                        new { name = "Mensaje", value = exception.Message, inline = false },
                        new
                        {
                            name = "StarkTrace (resumido)",
                            value = $"```\n{((exception.StackTrace ?? "No stack trace").Length > 1000 ? (exception.StackTrace ?? "No stack trace")[..1000] : (exception.StackTrace ?? "No stack trace"))}\n```",
                            inline = false
                        }
                    },
                    footer = new { text = $"TimeStamp: {DateTime.UtcNow}" }
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
                    "Fallo el envio del webhook a Discord. Codigo de status: {StatusCode}, Razon: {Reason}. Respuesta de Discord: {ErrorResponse}", 
                    response.StatusCode, 
                    response.ReasonPhrase,
                    errorResponse 
                );
            }
            else
            {
                _logger.LogInformation("Webhook de error enviado a Discord con éxito.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocurrio una excepcion al intentar enviar el webhook de error al servidor.");
        }
    }
}