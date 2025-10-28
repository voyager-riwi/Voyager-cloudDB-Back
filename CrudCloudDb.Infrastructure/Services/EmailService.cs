using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CrudCloudDb.Application.Services.Interfaces;
using CrudCloudDb.Application.DTOs.Email;
using CrudCloudDb.Application.Interfaces.Repositories;
using CrudCloudDb.Core.Entities;

namespace CrudCloudDb.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly IEmailLogRepository _emailLogRepository;
        private readonly ILogger<EmailService> _logger;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _senderEmail;
        private readonly string _senderName;
        private readonly string _username;
        private readonly string _password;

        public EmailService(
            IConfiguration configuration,
            IEmailLogRepository emailLogRepository,
            ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _emailLogRepository = emailLogRepository;
            _logger = logger;
            
            _smtpServer = _configuration["EmailSettings:SmtpServer"] ?? throw new InvalidOperationException("EmailSettings:SmtpServer is not configured.");
            _smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            _senderEmail = _configuration["EmailSettings:SenderEmail"] ?? throw new InvalidOperationException("EmailSettings:SenderEmail is not configured.");
            _senderName = _configuration["EmailSettings:SenderName"] ?? "CrudCloudDb";
            _username = _configuration["EmailSettings:Username"] ?? throw new InvalidOperationException("EmailSettings:Username is not configured.");
            _password = _configuration["EmailSettings:Password"] ?? throw new InvalidOperationException("EmailSettings:Password is not configured.");
        }

        public async Task SendDatabaseCreatedEmailAsync(DatabaseCreatedEmailDto dto)
        {
            var subject = $"✅ Database Created: {dto.DatabaseName}";
            
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .credentials {{ background: white; padding: 20px; border-left: 4px solid #667eea; margin: 20px 0; }}
        .credentials-item {{ margin: 10px 0; }}
        .credentials-label {{ font-weight: bold; color: #667eea; }}
        .credentials-value {{ font-family: 'Courier New', monospace; background: #f0f0f0; padding: 5px 10px; display: inline-block; border-radius: 3px; }}
        .warning {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Database Created Successfully!</h1>
        </div>
        <div class='content'>
            <p>Hi {dto.UserName},</p>
            <p>Your <strong>{dto.Engine}</strong> database <strong>{dto.DatabaseName}</strong> has been created successfully!</p>
            
            <div class='credentials'>
                <h3>📋 Connection Details</h3>
                <div class='credentials-item'>
                    <span class='credentials-label'>Engine:</span>
                    <span class='credentials-value'>{dto.Engine}</span>
                </div>
                <div class='credentials-item'>
                    <span class='credentials-label'>Database:</span>
                    <span class='credentials-value'>{dto.DatabaseName}</span>
                </div>
                <div class='credentials-item'>
                    <span class='credentials-label'>Username:</span>
                    <span class='credentials-value'>{dto.Username}</span>
                </div>
                <div class='credentials-item'>
                    <span class='credentials-label'>Password:</span>
                    <span class='credentials-value'>{dto.Password}</span>
                </div>
                <div class='credentials-item'>
                    <span class='credentials-label'>Port:</span>
                    <span class='credentials-value'>{dto.Port}</span>
                </div>
                <div class='credentials-item'>
                    <span class='credentials-label'>Connection String:</span>
                    <span class='credentials-value' style='word-break: break-all;'>{dto.ConnectionString}</span>
                </div>
            </div>

            <div class='warning'>
                <strong>⚠️ IMPORTANT:</strong> This is the only time you will see your password. Please save it in a secure location.
                If you lose it, you can reset it from your dashboard.
            </div>

            <p>Created at: {dto.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC</p>

            <div class='footer'>
                <p>This is an automated message from CrudCloudDb Platform</p>
                <p>If you didn't create this database, please contact support immediately.</p>
            </div>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(dto.UserEmail, subject, body);
        }

        public async Task SendDatabaseDeletedEmailAsync(DatabaseDeletedEmailDto dto)
        {
            var subject = $"🗑️ Database Deleted: {dto.DatabaseName}";
            
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .info {{ background: white; padding: 20px; border-left: 4px solid #f5576c; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🗑️ Database Deleted</h1>
        </div>
        <div class='content'>
            <p>Hi {dto.UserName},</p>
            <p>Your <strong>{dto.Engine}</strong> database <strong>{dto.DatabaseName}</strong> has been permanently deleted.</p>
            
            <div class='info'>
                <h3>📋 Deletion Details</h3>
                <p><strong>Database Name:</strong> {dto.DatabaseName}</p>
                <p><strong>Engine:</strong> {dto.Engine}</p>
                <p><strong>Deleted At:</strong> {dto.DeletedAt:yyyy-MM-dd HH:mm:ss} UTC</p>
            </div>

            <p>⚠️ All data associated with this database has been permanently removed and cannot be recovered.</p>

            <div class='footer'>
                <p>This is an automated message from CrudCloudDb Platform</p>
                <p>If you didn't delete this database, please contact support immediately.</p>
            </div>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(dto.UserEmail, subject, body);
        }

        public async Task SendPasswordResetEmailAsync(PasswordResetEmailDto dto)
        {
            var subject = $"🔑 Password Reset: {dto.DatabaseName}";
            
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #4facfe 0%, #00f2fe 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .credentials {{ background: white; padding: 20px; border-left: 4px solid #4facfe; margin: 20px 0; }}
        .credentials-item {{ margin: 10px 0; }}
        .credentials-label {{ font-weight: bold; color: #4facfe; }}
        .credentials-value {{ font-family: 'Courier New', monospace; background: #f0f0f0; padding: 5px 10px; display: inline-block; border-radius: 3px; }}
        .warning {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔑 Password Reset Successful</h1>
        </div>
        <div class='content'>
            <p>Hi {dto.UserName},</p>
            <p>Your password for database <strong>{dto.DatabaseName}</strong> has been reset successfully.</p>
            
            <div class='credentials'>
                <h3>📋 New Connection Details</h3>
                <div class='credentials-item'>
                    <span class='credentials-label'>Database:</span>
                    <span class='credentials-value'>{dto.DatabaseName}</span>
                </div>
                <div class='credentials-item'>
                    <span class='credentials-label'>Username:</span>
                    <span class='credentials-value'>{dto.NewUsername}</span>
                </div>
                <div class='credentials-item'>
                    <span class='credentials-label'>New Password:</span>
                    <span class='credentials-value'>{dto.NewPassword}</span>
                </div>
                <div class='credentials-item'>
                    <span class='credentials-label'>Connection String:</span>
                    <span class='credentials-value' style='word-break: break-all;'>{dto.ConnectionString}</span>
                </div>
            </div>

            <div class='warning'>
                <strong>⚠️ IMPORTANT:</strong> This is the only time you will see your new password. Please save it in a secure location.
                Your old password is no longer valid.
            </div>

            <p>Reset at: {dto.ResetAt:yyyy-MM-dd HH:mm:ss} UTC</p>

            <div class='footer'>
                <p>This is an automated message from CrudCloudDb Platform</p>
                <p>If you didn't reset this password, please contact support immediately.</p>
            </div>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(dto.UserEmail, subject, body);
        }

        public async Task SendWelcomeEmailAsync(string email, string userName)
        {
            var subject = "Welcome to CrudCloudDb! 🎉";
            
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Welcome to CrudCloudDb!</h1>
        </div>
        <div class='content'>
            <p>Hi {userName},</p>
            <p>Thank you for joining CrudCloudDb! We're excited to have you on board.</p>
            <p>With CrudCloudDb, you can easily create and manage databases on demand:</p>
            <ul>
                <li>✅ PostgreSQL</li>
                <li>✅ MySQL</li>
                <li>✅ MongoDB</li>
            </ul>
            <p>Get started by creating your first database from your dashboard!</p>
            <div class='footer'>
                <p>This is an automated message from CrudCloudDb Platform</p>
            </div>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var log = new EmailLog
            {
                To = to,
                Subject = subject,
                Body = body,
                SentAt = DateTime.UtcNow
            };

            try
            {
                using var smtpClient = new SmtpClient(_smtpServer, _smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(_username, _password)
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_senderEmail, _senderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(to);

                await smtpClient.SendMailAsync(mailMessage);

                log.IsSent = true;
                _logger.LogInformation("✅ Email sent successfully to {To}", to);
            }
            catch (Exception ex)
            {
                log.IsSent = false;
                log.ErrorMessage = ex.Message;
                _logger.LogError(ex, "❌ Failed to send email to {To}", to);
            }
            finally
            {
                await _emailLogRepository.AddAsync(log);
            }
        }
    }
}