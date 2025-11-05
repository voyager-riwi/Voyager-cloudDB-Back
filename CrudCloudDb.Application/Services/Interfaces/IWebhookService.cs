namespace CrudCloudDb.Application.Services.Interfaces;

public interface IWebhookService
{
    Task SendErrorNotificationAsync(Exception exception, string contextMessage);
}