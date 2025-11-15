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
        /// Marca una base de datos como eliminada (soft delete - 30 días de gracia)
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <param name="databaseId">ID de la base de datos</param>
        /// <returns>True si se marcó como eliminada correctamente</returns>
        Task<bool> DeleteDatabaseAsync(Guid userId, Guid databaseId);

        /// <summary>
        /// Restaura una base de datos marcada como eliminada (dentro del período de gracia de 30 días)
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <param name="databaseId">ID de la base de datos</param>
        /// <returns>True si se restauró correctamente</returns>
        Task<bool> RestoreDatabaseAsync(Guid userId, Guid databaseId);

        /// <summary>
        /// Resetea el password de una base de datos
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <param name="databaseId">ID de la base de datos</param>
        Task ResetPasswordAsync(Guid userId, Guid databaseId);
    }
}