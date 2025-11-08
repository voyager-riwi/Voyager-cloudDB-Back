using CrudCloudDb.Application.DTOs.Webhook;

namespace CrudCloudDb.Application.Services.Interfaces;

public interface IWebhookService
{
    Task ProcessMercadoPagoNotificationAsync(MercadoPagoNotification notification);
    Task SendErrorNotificationAsync(Exception exception, string contextMessage);
    Task SendSuccesNotificationAsync(string title, string message);
    Task SendWarningNotificationAsync(string title, string message);
}