using CrudCloudDb.Application.DTOs.Email;

namespace CrudCloudDb.Application.Services.Interfaces
{
    /// <summary>
    /// Servicio para envío de emails
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Envía email de bienvenida al crear cuenta
        /// </summary>
        Task SendAccountCreatedEmailAsync(AccountCreatedEmailDto emailDto);

        /// <summary>
        /// Envía email al crear base de datos con credenciales
        /// </summary>
        Task SendDatabaseCreatedEmailAsync(DatabaseCreatedEmailDto emailDto);

        /// <summary>
        /// Envía email al eliminar base de datos
        /// </summary>
        Task SendDatabaseDeletedEmailAsync(DatabaseDeletedEmailDto emailDto);

        /// <summary>
        /// Envía email al cambiar o renovar plan
        /// </summary>
        Task SendPlanChangedEmailAsync(PlanChangedEmailDto emailDto);

        /// <summary>
        /// Envía email al resetear password de base de datos
        /// </summary>
        Task SendPasswordResetEmailAsync(PasswordResetEmailDto emailDto);
    }
}