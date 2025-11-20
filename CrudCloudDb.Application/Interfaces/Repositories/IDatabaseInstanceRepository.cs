using CrudCloudDb.Core.Entities;

namespace CrudCloudDb.Application.Interfaces.Repositories
{
    /// <summary>
    /// Interfaz para el repositorio de instancias de bases de datos
    /// </summary>
    public interface IDatabaseInstanceRepository
    {
        /// <summary>
        /// Crea una nueva instancia de base de datos
        /// </summary>
        Task<DatabaseInstance> CreateAsync(DatabaseInstance databaseInstance);

        /// <summary>
        /// Obtiene una instancia de base de datos por ID
        /// </summary>
        Task<DatabaseInstance?> GetByIdAsync(Guid id);

        /// <summary>
        /// Obtiene una instancia de base de datos por container ID
        /// </summary>
        Task<DatabaseInstance?> GetByContainerIdAsync(string containerId);

        /// <summary>
        /// Obtiene todas las bases de datos de un usuario
        /// </summary>
        Task<IEnumerable<DatabaseInstance>> GetByUserIdAsync(Guid userId);

        /// <summary>
        /// Obtiene todas las bases de datos activas
        /// </summary>
        Task<IEnumerable<DatabaseInstance>> GetAllActiveAsync();

        /// <summary>
        /// Actualiza una instancia de base de datos
        /// </summary>
        Task UpdateAsync(DatabaseInstance databaseInstance);

        /// <summary>
        /// Elimina una instancia de base de datos
        /// </summary>
        Task DeleteAsync(Guid id);
    }
}