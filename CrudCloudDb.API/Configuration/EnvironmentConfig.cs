using Microsoft.Extensions.Configuration;

namespace CrudCloudDb.API.Configuration
{
    /// <summary>
    /// Servicio para leer configuraciones de variables de entorno
    /// </summary>
    public static class EnvironmentConfig
    {
        /// <summary>
        /// Obtiene el host de base de datos para connection strings de usuarios
        /// </summary>
        public static string GetDatabaseHost(IConfiguration configuration, string engine)
        {
            var envVar = $"DB_HOST_{engine.ToUpperInvariant()}";
            var host = Environment.GetEnvironmentVariable(envVar) 
                      ?? configuration[$"DatabaseHosts:{engine}"];
            
            if (string.IsNullOrEmpty(host))
            {
                return "localhost";
            }
            
            return host;
        }

        /// <summary>
        /// Obtiene la configuración de JWT
        /// </summary>
        public static (string Secret, string Issuer, string Audience) GetJwtConfig(IConfiguration configuration)
        {
            var secret = Environment.GetEnvironmentVariable("JWT_SECRET") 
                        ?? configuration["JwtSettings:Secret"];
            var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
                        ?? configuration["JwtSettings:Issuer"];
            var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
                          ?? configuration["JwtSettings:Audience"];
            
            if (string.IsNullOrEmpty(secret))
                throw new InvalidOperationException("JWT Secret is not configured");
            
            return (secret, issuer ?? "CrudCloudDb.API", audience ?? "CrudCloudDb.Frontend");
        }

        /// <summary>
        /// Obtiene la configuración de Email
        /// </summary>
        public static EmailConfig GetEmailConfig(IConfiguration configuration)
        {
            return new EmailConfig
            {
                SmtpServer = Environment.GetEnvironmentVariable("SMTP_SERVER") 
                            ?? configuration["EmailSettings:SmtpServer"] 
                            ?? "smtp.gmail.com",
                SmtpPort = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT") 
                                    ?? configuration["EmailSettings:SmtpPort"] 
                                    ?? "587"),
                SenderEmail = Environment.GetEnvironmentVariable("SMTP_SENDER_EMAIL") 
                             ?? configuration["EmailSettings:SenderEmail"],
                SenderName = Environment.GetEnvironmentVariable("SMTP_SENDER_NAME") 
                            ?? configuration["EmailSettings:SenderName"] 
                            ?? "PotterCloud",
                Username = Environment.GetEnvironmentVariable("SMTP_USERNAME") 
                          ?? configuration["EmailSettings:Username"],
                Password = Environment.GetEnvironmentVariable("SMTP_PASSWORD") 
                          ?? configuration["EmailSettings:Password"],
                EnableSsl = bool.Parse(Environment.GetEnvironmentVariable("SMTP_ENABLE_SSL") 
                                      ?? configuration["EmailSettings:EnableSsl"] 
                                      ?? "true")
            };
        }

        /// <summary>
        /// Obtiene la configuración de Webhook
        /// </summary>
        public static string GetDiscordWebhookUrl(IConfiguration configuration)
        {
            return Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_URL") 
                   ?? configuration["WebhookSettings:DiscordUrl"] 
                   ?? string.Empty;
        }
    }

    public class EmailConfig
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool EnableSsl { get; set; }
    }
}

