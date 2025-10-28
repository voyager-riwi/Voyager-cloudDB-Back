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
        /// Crea un nuevo contenedor de base de datos
        /// </summary>
        Task<DatabaseInstance> CreateDatabaseContainerAsync(
            User user,
            DatabaseEngine engine,
            string databaseName);

        /// <summary>
        /// Detiene un contenedor
        /// </summary>
        Task<bool> StopContainerAsync(string containerId);

        /// <summary>
        /// Inicia un contenedor detenido
        /// </summary>
        Task<bool> StartContainerAsync(string containerId);

        /// <summary>
        /// Elimina un contenedor permanentemente
        /// </summary>
        Task<bool> RemoveContainerAsync(string containerId);

        /// <summary>
        /// Verifica si un contenedor está corriendo
        /// </summary>
        Task<bool> IsContainerRunningAsync(string containerId);

        /// <summary>
        /// Obtiene los logs de un contenedor
        /// </summary>
        Task<string> GetContainerLogsAsync(string containerId, int lines = 100);

        /// <summary>
        /// Resetea la contraseña de una base de datos
        /// </summary>
        Task<PasswordResetResult> ResetDatabasePasswordAsync(
            DatabaseInstance dbInstance,
            User user);
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