using CrudCloudDb.Application.DTOs.Database;

namespace CrudCloudDb.Application.Services.Interfaces
{
    /// <summary>
    /// Interfaz para el servicio de gestión de bases de datos
    /// </summary>
    public interface IDatabaseService
    {
        /// <summary>
        /// Crea una nueva base de datos para un usuario
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <param name="request">Datos de la base de datos a crear</param>
        /// <returns>Información de la base de datos creada</returns>
        Task<DatabaseResponseDto> CreateDatabaseAsync(Guid userId, CreateDatabaseRequestDto request);

        /// <summary>
        /// Obtiene todas las bases de datos de un usuario
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <returns>Lista de bases de datos</returns>
        Task<List<DatabaseResponseDto>> GetUserDatabasesAsync(Guid userId);

        /// <summary>
        /// Obtiene los detalles de una base de datos específica
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <param name="databaseId">ID de la base de datos</param>
        /// <returns>Detalles de la base de datos o null si no existe</returns>
        Task<DatabaseResponseDto?> GetDatabaseByIdAsync(Guid userId, Guid databaseId);

        /// <summary>
        /// Elimina una base de datos permanentemente
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <param name="databaseId">ID de la base de datos</param>
        /// <returns>True si se eliminó correctamente</returns>
        Task<bool> DeleteDatabaseAsync(Guid userId, Guid databaseId);

        /// <summary>
        /// Resetea el password de una base de datos
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <param name="databaseId">ID de la base de datos</param>
        Task ResetPasswordAsync(Guid userId, Guid databaseId);

        /// <summary>
        /// Inicia un contenedor detenido
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <param name="databaseId">ID de la base de datos</param>
        /// <returns>True si se inició correctamente</returns>
        Task<bool> StartDatabaseAsync(Guid userId, Guid databaseId);

        /// <summary>
        /// Detiene un contenedor en ejecución
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <param name="databaseId">ID de la base de datos</param>
        /// <returns>True si se detuvo correctamente</returns>
        Task<bool> StopDatabaseAsync(Guid userId, Guid databaseId);
    }
}