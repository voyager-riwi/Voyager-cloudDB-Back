using CrudCloudDb.Application.DTOs.Webhook;

namespace CrudCloudDb.Application.Services.Interfaces;

public interface IWebhookService
{
    Task ProcessMercadoPagoNotificationAsync(MercadoPagoNotification notification);
    Task SendErrorNotificationAsync(Exception exception, string contextMessage);
}