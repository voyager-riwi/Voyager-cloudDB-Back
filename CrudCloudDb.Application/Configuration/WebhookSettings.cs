namespace CrudCloudDb.Application.Configuration;

public class WebhookSettings
{
    public string DiscordUrl { get; set; } =  string.Empty;
    public string SignatureSecret { get; set; } = string.Empty;
}