using CrudCloudDb.Application.Configuration;
using CrudCloudDb.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;


namespace CrudCloudDb.Application.Services.Implementation;

public class WebhookService : IWebhookService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly WebhookSettings _webhookSettings;
    private readonly ILogger<WebhookService> _logger;
    
    public WebhookService(
        IHttpClientFactory httpClientFactory, 
        IOptions<WebhookSettings> webhookSettings,
        ILogger<WebhookService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _webhookSettings = webhookSettings.Value;
        _logger = logger;
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
            embeds = new[]{
            new{
                    title = "Error inesperado en la API",
                    description = $"**Contexto:** {contextMessage}",
                    color = 15548997,
                    field = new[]
                    {
                        new {name = "Tipo de Error", value = exception.GetType().Name, inline = true},
                        new {name = "Mensaje", value = exception.Message, inline = false},
                        new
                        {
                            name = "StarkTrace (resumido)",
                            value = $"```\n{(exception.StackTrace == null ? "No stack trace" : exception.StackTrace[..Math.Min(exception.StackTrace.Length, 1000)])}...\n```",
                            inline = false
                        }
                    },
                    footer = new{ text = $"TimeStamp: {DateTime.UtcNow}"}
               }   
            }
        };
        
        var jsonPayload  = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        try
        {
            var response = await httpClient.PostAsync(_webhookSettings.DiscordUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Fallo el envio del webhook al discord. Codigo de status: {StatusCode}, Razon: {Reason}", response.StatusCode, response.ReasonPhrase);
                
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocurrio una excepcion al intentar enviar el webhook de error al servidor.");
        }
    }
}