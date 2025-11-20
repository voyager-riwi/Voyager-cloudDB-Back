﻿using System.Text.Json.Serialization;

namespace CrudCloudDb.Application.DTOs.Webhook;

public class MercadoPagoNotification
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("live_mode")]
    public bool LiveMode { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("date_created")]
    public string DateCreated { get; set; } = string.Empty;

    [JsonPropertyName("user_id")]
    public long? UserId { get; set; }

    [JsonPropertyName("api_version")]
    public string ApiVersion { get; set; } = string.Empty;

    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("data")]
    public NotificationData? Data { get; set; }

    // Campos legacy para compatibilidad
    [JsonPropertyName("resource")]
    public string Resource { get; set; } = string.Empty;

    [JsonPropertyName("topic")]
    public string Topic { get; set; } = string.Empty;
}

public class NotificationData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}
