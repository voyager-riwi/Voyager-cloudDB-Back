using CrudCloudDb.Application.Services.Interfaces;
using CrudCloudDb.Application.DTOs.Email;
using CrudCloudDb.Application.Interfaces.Repositories;
using CrudCloudDb.Core.Entities;
using CrudCloudDb.Core.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace CrudCloudDb.Infrastructure.Services
{
    /// <summary>
    /// Servicio de envío de emails usando MailKit
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly IEmailLogRepository _emailLogRepository;
        private readonly ILogger<EmailService> _logger;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _senderEmail;
        private readonly string _senderName;
        private readonly string _username;
        private readonly string _password;
        private readonly bool _enableSsl;

        public EmailService(
            IConfiguration configuration,
            IEmailLogRepository emailLogRepository,
            ILogger<EmailService> logger)
        {
            _emailLogRepository = emailLogRepository;
            _logger = logger;

            // ⭐ Sintaxis correcta con valores por defecto
            _smtpServer = configuration["EmailSettings:SmtpServer"]
                          ?? throw new InvalidOperationException("EmailSettings:SmtpServer is not configured.");
    
            // ⭐ GetValue necesita el valor por defecto como segundo parámetro
            var smtpPortString = configuration["EmailSettings:SmtpPort"];
            _smtpPort = !string.IsNullOrEmpty(smtpPortString) 
                ? int.Parse(smtpPortString) 
                : 587;
    
            _senderEmail = configuration["EmailSettings:SenderEmail"]
                           ?? throw new InvalidOperationException("EmailSettings:SenderEmail is not configured.");
    
            _senderName = configuration["EmailSettings:SenderName"] ?? "PotterCloud";
    
            _username = configuration["EmailSettings:Username"]
                        ?? throw new InvalidOperationException("EmailSettings:Username is not configured.");
    
            _password = configuration["EmailSettings:Password"]
                        ?? throw new InvalidOperationException("EmailSettings:Password is not configured.");
    
            // ⭐ Para bool también
            var enableSslString = configuration["EmailSettings:EnableSsl"];
            _enableSsl = !string.IsNullOrEmpty(enableSslString) 
                ? bool.Parse(enableSslString) 
                : true;
        }

        // ============================================
        // 1️⃣ EMAIL: CUENTA CREADA
        // ============================================
        public async Task SendAccountCreatedEmailAsync(AccountCreatedEmailDto emailDto)
        {
            try
            {
                _logger.LogInformation($"📧 Sending account created email to {emailDto.UserEmail}");

                var subject = "¡Bienvenido a PotterCloud! 🎉";
                var body = BuildAccountCreatedEmailBody(emailDto);

                await SendEmailAsync(emailDto.UserEmail, subject, body, EmailType.AccountCreated);

                _logger.LogInformation($"✅ Account created email sent to {emailDto.UserEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending account created email to {emailDto.UserEmail}");
                throw;
            }
        }

        // ============================================
        // 2️⃣ EMAIL: BASE DE DATOS CREADA
        // ============================================
        public async Task SendDatabaseCreatedEmailAsync(DatabaseCreatedEmailDto emailDto)
        {
            try
            {
                _logger.LogInformation($"📧 Sending database created email to {emailDto.UserEmail}");

                var subject = $"Tu base de datos {emailDto.Engine} fue creada exitosamente 🗄️";
                var body = BuildDatabaseCreatedEmailBody(emailDto);

                await SendEmailAsync(emailDto.UserEmail, subject, body, EmailType.DatabaseCreated);

                _logger.LogInformation($"✅ Database created email sent to {emailDto.UserEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending database created email to {emailDto.UserEmail}");
                throw;
            }
        }

        // ============================================
        // 3️⃣ EMAIL: BASE DE DATOS ELIMINADA
        // ============================================
        public async Task SendDatabaseDeletedEmailAsync(DatabaseDeletedEmailDto emailDto)
        {
            try
            {
                _logger.LogInformation($"📧 Sending database deleted email to {emailDto.UserEmail}");

                var subject = $"Base de datos {emailDto.DatabaseName} eliminada 🗑️";
                var body = BuildDatabaseDeletedEmailBody(emailDto);

                await SendEmailAsync(emailDto.UserEmail, subject, body, EmailType.DatabaseDeleted);

                _logger.LogInformation($"✅ Database deleted email sent to {emailDto.UserEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending database deleted email to {emailDto.UserEmail}");
                throw;
            }
        }

        // ============================================
        // 4️⃣ EMAIL: PLAN CAMBIADO/RENOVADO
        // ============================================
        public async Task SendPlanChangedEmailAsync(PlanChangedEmailDto emailDto)
        {
            try
            {
                _logger.LogInformation($"📧 Sending plan changed email to {emailDto.UserEmail}");

                var subject = emailDto.IsRenewal
                    ? $"Tu plan {emailDto.NewPlanName} ha sido renovado ✨"
                    : $"Has cambiado al plan {emailDto.NewPlanName} 🚀";

                var body = BuildPlanChangedEmailBody(emailDto);

                await SendEmailAsync(emailDto.UserEmail, subject, body, EmailType.PlanChanged);

                _logger.LogInformation($"✅ Plan changed email sent to {emailDto.UserEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending plan changed email to {emailDto.UserEmail}");
                throw;
            }
        }

        // ============================================
        // 5️⃣ EMAIL: PASSWORD RESET
        // ============================================
        public async Task SendPasswordResetEmailAsync(PasswordResetEmailDto emailDto)
        {
            try
            {
                _logger.LogInformation($"📧 Sending password reset email to {emailDto.UserEmail}");

                var subject = $"Password de {emailDto.DatabaseName} reseteado 🔑";
                var body = BuildPasswordResetEmailBody(emailDto);

                await SendEmailAsync(emailDto.UserEmail, subject, body, EmailType.PasswordReset);

                _logger.LogInformation($"✅ Password reset email sent to {emailDto.UserEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending password reset email to {emailDto.UserEmail}");
                throw;
            }
        }

        // ============================================
        // MÉTODO PRIVADO: ENVIAR EMAIL CON MAILKIT
        // ============================================
        private async Task SendEmailAsync(string toEmail, string subject, string body, EmailType emailType)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_senderName, _senderEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = body
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            try
            {
                // Conectar al servidor SMTP
                await client.ConnectAsync(_smtpServer, _smtpPort, SecureSocketOptions.StartTls);

                // Autenticar
                await client.AuthenticateAsync(_username, _password);

                // Enviar
                await client.SendAsync(message);

                // Desconectar
                await client.DisconnectAsync(true);

                // Registrar en BD
                await LogEmailAsync(toEmail, subject, body, emailType, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending email via SMTP");
                await LogEmailAsync(toEmail, subject, body, emailType, false, ex.Message);
                throw;
            }
        }

        // ============================================
        // MÉTODO PRIVADO: REGISTRAR EMAIL EN BD
        // ============================================
        private async Task LogEmailAsync(string toEmail, string subject, string body, EmailType emailType, bool success, string errorMessage = null)
        {
            try
            {
                var emailLog = new EmailLog
                {
                    Id = Guid.NewGuid(),
                    ToEmail = toEmail,
                    Subject = subject,
                    Body = body,
                    EmailType = emailType,
                    SentAt = DateTime.UtcNow,
                    Success = success,
                    ErrorMessage = errorMessage
                };

                await _emailLogRepository.CreateAsync(emailLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging email");
            }
        }

        // ============================================
        // HTML TEMPLATES
        // ============================================

        private string BuildAccountCreatedEmailBody(AccountCreatedEmailDto dto)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; border-radius: 10px; padding: 30px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .header h1 {{ color: #4F46E5; margin: 0; }}
        .content {{ line-height: 1.6; color: #333; }}
        .highlight {{ background-color: #F3F4F6; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #E5E7EB; color: #6B7280; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 ¡Bienvenido a PotterCloud!</h1>
        </div>
        
        <div class='content'>
            <p>Hola <strong>{dto.UserName}</strong>,</p>
            
            <p>¡Tu cuenta ha sido creada exitosamente! Estamos emocionados de tenerte con nosotros.</p>
            
            <div class='highlight'>
                <p><strong>Fecha de registro:</strong> {dto.CreatedAt:dd/MM/yyyy HH:mm}</p>
                <p><strong>Email:</strong> {dto.UserEmail}</p>
            </div>
            
            <p>Con PotterCloud puedes:</p>
            <ul>
                <li>✨ Crear bases de datos PostgreSQL, MySQL y MongoDB</li>
                <li>🔐 Gestionar credenciales de forma segura</li>
                <li>📊 Monitorear el uso de tus recursos</li>
                <li>🚀 Escalar según tus necesidades</li>
            </ul>
            
            <p>¡Comienza creando tu primera base de datos ahora!</p>
            
            <p>Si tienes alguna pregunta, no dudes en contactarnos.</p>
            
            <p>Saludos,<br><strong>Equipo de PotterCloud</strong></p>
        </div>
        
        <div class='footer'>
            <p>Este es un email automático, por favor no respondas a este mensaje.</p>
            <p>&copy; 2024 PotterCloud. Todos los derechos reservados.</p>
        </div>
    </div>
</body>
</html>";
        }

private string BuildDatabaseCreatedEmailBody(DatabaseCreatedEmailDto dto)
{
    // Determinar colores según el motor
    var (primaryColor, bgColor, borderColor) = dto.Engine.ToLower() switch
    {
        "postgresql" => ("#3B82F6", "#EFF6FF", "#3B82F6"), // Azul
        "mysql" => ("#F97316", "#FFF7ED", "#F97316"),      // Naranja
        "mongodb" => ("#10B981", "#F0FDF4", "#10B981"),    // Verde
        _ => ("#6B7280", "#F3F4F6", "#6B7280")             // Gris por defecto
    };

    // Extraer host real del connection string
    var host = "91.98.42.248"; // Valor por defecto
    if (!string.IsNullOrEmpty(dto.ConnectionString))
    {
        // Intentar extraer el host del connection string
        if (dto.ConnectionString.Contains("Host="))
        {
            var hostPart = dto.ConnectionString.Split(';')
                .FirstOrDefault(p => p.Trim().StartsWith("Host="));
            if (hostPart != null)
                host = hostPart.Split('=')[1].Trim();
        }
        else if (dto.ConnectionString.Contains("Server="))
        {
            var serverPart = dto.ConnectionString.Split(';')
                .FirstOrDefault(p => p.Trim().StartsWith("Server="));
            if (serverPart != null)
                host = serverPart.Split('=')[1].Trim();
        }
        else if (dto.ConnectionString.Contains("@"))
        {
            // MongoDB: mongodb://user:pass@HOST:port/db
            var atIndex = dto.ConnectionString.IndexOf("@");
            var colonIndex = dto.ConnectionString.IndexOf(":", atIndex);
            if (atIndex > 0 && colonIndex > atIndex)
                host = dto.ConnectionString.Substring(atIndex + 1, colonIndex - atIndex - 1);
        }
    }

    return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; border-radius: 10px; padding: 30px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .header h1 {{ color: {primaryColor}; margin: 0; }}
        .content {{ line-height: 1.6; color: #333; }}
        .credentials {{ background-color: {bgColor}; border-left: 4px solid {borderColor}; padding: 20px; margin: 20px 0; border-radius: 5px; }}
        .credentials h3 {{ margin-top: 0; color: {primaryColor}; }}
        .credential-item {{ margin: 10px 0; }}
        .credential-label {{ font-weight: bold; color: {primaryColor}; }}
        .credential-value {{ font-family: 'Courier New', monospace; background-color: white; padding: 5px 10px; border-radius: 3px; display: inline-block; }}
        .warning {{ background-color: #FEF3C7; border-left: 4px solid #F59E0B; padding: 15px; margin: 20px 0; border-radius: 5px; }}
        .footer {{ text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #E5E7EB; color: #6B7280; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🗄️ Base de datos creada</h1>
        </div>
        
        <div class='content'>
            <p>Hola <strong>{dto.UserName}</strong>,</p>
            
            <p>Tu base de datos <strong>{dto.Engine}</strong> ha sido creada exitosamente.</p>
            
            <div class='credentials'>
                <h3>📋 Credenciales de acceso</h3>
                
                <div class='credential-item'>
                    <span class='credential-label'>🗄️ Gestor:</span>
                    <span class='credential-value'>{dto.Engine}</span>
                </div>
                
                <div class='credential-item'>
                    <span class='credential-label'>📦 Base de datos:</span>
                    <span class='credential-value'>{dto.DatabaseName}</span>
                </div>
                
                <div class='credential-item'>
                    <span class='credential-label'>👤 Usuario:</span>
                    <span class='credential-value'>{dto.Username}</span>
                </div>
                
                <div class='credential-item'>
                    <span class='credential-label'>🔑 Password:</span>
                    <span class='credential-value'>{dto.Password}</span>
                </div>
                
                <div class='credential-item'>
                    <span class='credential-label'>🌐 Host:</span>
                    <span class='credential-value'>{host}</span>
                </div>
                
                <div class='credential-item'>
                    <span class='credential-label'>🔌 Puerto:</span>
                    <span class='credential-value'>{dto.Port}</span>
                </div>
                
                <div class='credential-item' style='margin-top: 20px;'>
                    <span class='credential-label'>🔗 Connection String:</span><br>
                    <span class='credential-value' style='display: block; margin-top: 5px; word-break: break-all;'>{dto.ConnectionString}</span>
                </div>
            </div>
            
            <div class='warning'>
                <strong>⚠️ IMPORTANTE:</strong> Guarda estas credenciales de forma segura. Por razones de seguridad, no podrás volver a verlas en el panel.
            </div>
            
            <p><strong>Fecha de creación:</strong> {dto.CreatedAt:dd/MM/yyyy HH:mm}</p>
            
            <p>Puedes conectarte usando estas credenciales desde cualquier cliente de {dto.Engine} como DBeaver, pgAdmin, MySQL Workbench, Compass, etc.</p>
            
            <p>Saludos,<br><strong>Equipo de PotterCloud</strong></p>
        </div>
        
        <div class='footer'>
            <p>Este es un email automático, por favor no respondas a este mensaje.</p>
            <p>&copy; 2024 PotterCloud. Todos los derechos reservados.</p>
        </div>
    </div>
</body>
</html>";
}

        private string BuildDatabaseDeletedEmailBody(DatabaseDeletedEmailDto dto)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; border-radius: 10px; padding: 30px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .header h1 {{ color: #EF4444; margin: 0; }}
        .content {{ line-height: 1.6; color: #333; }}
        .info {{ background-color: #FEE2E2; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #E5E7EB; color: #6B7280; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🗑️ Base de datos eliminada</h1>
        </div>
        
        <div class='content'>
            <p>Hola <strong>{dto.UserName}</strong>,</p>
            
            <p>Tu base de datos <strong>{dto.DatabaseName}</strong> ({dto.Engine}) ha sido eliminada permanentemente.</p>
            
            <div class='info'>
                <p><strong>Base de datos:</strong> {dto.DatabaseName}</p>
                <p><strong>Motor:</strong> {dto.Engine}</p>
                <p><strong>Fecha de eliminación:</strong> {dto.DeletedAt:dd/MM/yyyy HH:mm}</p>
            </div>
            
            <p>⚠️ Esta acción es irreversible. Todos los datos han sido eliminados permanentemente.</p>
            
            <p>Si eliminaste esta base de datos por error, por favor contacta a soporte inmediatamente.</p>
            
            <p>Saludos,<br><strong>Equipo de PotterCloud</strong></p>
        </div>
        
        <div class='footer'>
            <p>Este es un email automático, por favor no respondas a este mensaje.</p>
            <p>&copy; 2024 PotterCloud. Todos los derechos reservados.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string BuildPlanChangedEmailBody(PlanChangedEmailDto dto)
        {
            var action = dto.IsRenewal ? "renovado" : "cambiado";
            
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; border-radius: 10px; padding: 30px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .header h1 {{ color: #8B5CF6; margin: 0; }}
        .content {{ line-height: 1.6; color: #333; }}
        .plan-info {{ background-color: #F5F3FF; padding: 20px; border-radius: 5px; margin: 20px 0; }}
        .price {{ font-size: 24px; font-weight: bold; color: #8B5CF6; }}
        .footer {{ text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #E5E7EB; color: #6B7280; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🚀 Tu plan ha sido {action}</h1>
        </div>
        
        <div class='content'>
            <p>Hola <strong>{dto.UserName}</strong>,</p>
            
            <p>Tu suscripción ha sido {action} exitosamente.</p>
            
            <div class='plan-info'>
                {(!dto.IsRenewal ? $"<p><strong>Plan anterior:</strong> {dto.OldPlanName}</p>" : "")}
                <p><strong>Plan actual:</strong> {dto.NewPlanName}</p>
                <p class='price'>${dto.NewPlanPrice:N2} / mes</p>
                <p><strong>Fecha del cambio:</strong> {dto.ChangedAt:dd/MM/yyyy HH:mm}</p>
                {(dto.NextBillingDate.HasValue ? $"<p><strong>Próxima facturación:</strong> {dto.NextBillingDate.Value:dd/MM/yyyy}</p>" : "")}
            </div>
            
            <p>¡Gracias por confiar en PotterCloud! 💜</p>
            
            <p>Saludos,<br><strong>Equipo de PotterCloud</strong></p>
        </div>
        
        <div class='footer'>
            <p>Este es un email automático, por favor no respondas a este mensaje.</p>
            <p>&copy; 2024 PotterCloud. Todos los derechos reservados.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string BuildPasswordResetEmailBody(PasswordResetEmailDto dto)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; border-radius: 10px; padding: 30px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .header h1 {{ color: #F59E0B; margin: 0; }}
        .content {{ line-height: 1.6; color: #333; }}
        .credentials {{ background-color: #FFFBEB; border-left: 4px solid #F59E0B; padding: 20px; margin: 20px 0; border-radius: 5px; }}
        .credentials h3 {{ margin-top: 0; color: #F59E0B; }}
        .credential-item {{ margin: 10px 0; }}
        .credential-label {{ font-weight: bold; color: #D97706; }}
        .credential-value {{ font-family: 'Courier New', monospace; background-color: white; padding: 5px 10px; border-radius: 3px; display: inline-block; }}
        .footer {{ text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #E5E7EB; color: #6B7280; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔑 Password reseteado</h1>
        </div>
        
        <div class='content'>
            <p>Hola <strong>{dto.UserName}</strong>,</p>
            
            <p>El password de tu base de datos <strong>{dto.DatabaseName}</strong> ha sido reseteado.</p>
            
            <div class='credentials'>
                <h3>🔐 Nuevas credenciales</h3>
                
                <div class='credential-item'>
                    <span class='credential-label'>👤 Usuario:</span>
                    <span class='credential-value'>{dto.NewUsername}</span>
                </div>
                
                <div class='credential-item'>
                    <span class='credential-label'>🔑 Nuevo Password:</span>
                    <span class='credential-value'>{dto.NewPassword}</span>
                </div>
                
                <div class='credential-item' style='margin-top: 20px;'>
                    <span class='credential-label'>🔗 Connection String:</span><br>
                    <span class='credential-value' style='display: block; margin-top: 5px; word-break: break-all;'>{dto.ConnectionString}</span>
                </div>
            </div>
            
            <p><strong>Fecha del reset:</strong> {dto.ResetAt:dd/MM/yyyy HH:mm}</p>
            
            <p>⚠️ Guarda estas credenciales en un lugar seguro.</p>
            
            <p>Saludos,<br><strong>Equipo de PotterCloud</strong></p>
        </div>
        
        <div class='footer'>
            <p>Este es un email automático, por favor no respondas a este mensaje.</p>
            <p>&copy; 2024 PotterCloud. Todos los derechos reservados.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}