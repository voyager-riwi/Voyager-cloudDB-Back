namespace CrudCloudDb.API.Configuration;

public class MercadoPagoSettings
{
    public string AccessToken { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string NotificationUrl { get; set; } = string.Empty;
}