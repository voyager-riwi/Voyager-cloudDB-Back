using System.Text.Json.Serialization;

namespace CrudCloudDb.Application.DTOs.Webhook;

public class MercadoPagoNotification
{
    [JsonPropertyName("resource")]
    public string Resource { get; set; } = string.Empty;

    [JsonPropertyName("topic")]
    public string Topic { get; set; } = string.Empty;
}