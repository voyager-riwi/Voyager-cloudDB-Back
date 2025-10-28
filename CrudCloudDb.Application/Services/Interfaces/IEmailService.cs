// CrudCloudDb.Application/Services/Interfaces/IEmailService.cs

using CrudCloudDb.Application.DTOs.Email;

namespace CrudCloudDb.Application.Services.Interfaces
{
    public interface IEmailService
    {
        /// <summary>
        /// Envía email cuando se crea una base de datos
        /// </summary>
        Task SendDatabaseCreatedEmailAsync(DatabaseCreatedEmailDto dto);

        /// <summary>
        /// Envía email cuando se elimina una base de datos
        /// </summary>
        Task SendDatabaseDeletedEmailAsync(DatabaseDeletedEmailDto dto);

        /// <summary>
        /// Envía email cuando se resetea la contraseña
        /// </summary>
        Task SendPasswordResetEmailAsync(PasswordResetEmailDto dto);

        /// <summary>
        /// Envía email de bienvenida (puede ser útil)
        /// </summary>
        Task SendWelcomeEmailAsync(string email, string userName);

        /// <summary>
        /// Envía email genérico
        /// </summary>
        Task SendEmailAsync(string to, string subject, string body);
    }
}