// CrudCloudDb.Application/Services/Interfaces/IDockerService.cs

using CrudCloudDb.Core.Entities;
using CrudCloudDb.Core.Enums;

namespace CrudCloudDb.Application.Services.Interfaces
{
    /// <summary>
    /// Servicio para gestión de contenedores Docker
    /// </summary>
    public interface IDockerService
    {
        /// <summary>
        /// Crea una nueva base de datos dentro de un contenedor maestro
        /// </summary>
        Task<DatabaseInstance> CreateDatabaseContainerAsync(
            User user,
            DatabaseEngine engine,
            string databaseName);

        /// <summary>
        /// Verifica si un contenedor maestro está corriendo
        /// </summary>
        Task<bool> IsContainerRunningAsync(string containerId);

        /// <summary>
        /// Obtiene los logs de un contenedor maestro
        /// </summary>
        Task<string> GetContainerLogsAsync(string containerId, int lines = 100);

        /// <summary>
        /// Resetea la contraseña de una base de datos
        /// </summary>
        Task<PasswordResetResult> ResetDatabasePasswordAsync(
            DatabaseInstance dbInstance,
            User user);

        /// <summary>
        /// Marca una base de datos como eliminada (no la elimina físicamente todavía)
        /// </summary>
        Task<bool> DeleteDatabaseAsync(DatabaseInstance database, User user);

        /// <summary>
        /// Elimina permanentemente una base de datos del contenedor maestro (después del período de gracia)
        /// </summary>
        Task PermanentlyDeleteDatabaseAsync(DatabaseEngine engine, string dbName, string username);

        /// <summary>
        /// Obtiene información del contenedor maestro para un motor específico
        /// </summary>
        Task<MasterContainerInfo?> GetMasterContainerInfoAsync(DatabaseEngine engine);

        /// <summary>
        /// Resetea la password de un usuario directamente en el contenedor maestro (sin enviar email)
        /// </summary>
        Task ResetPasswordInMasterAsync(
            MasterContainerInfo masterContainer,
            string username,
            string newPassword,
            DatabaseEngine engine);
    }


    /// <summary>
    /// Resultado de reseteo de contraseña
    /// </summary>
    public class PasswordResetResult
    {
        public bool Success { get; set; }
        public string NewPassword { get; set; } = string.Empty;
        public string NewPasswordHash { get; set; } = string.Empty;
        public string NewConnectionString { get; set; } = string.Empty;
    }
}