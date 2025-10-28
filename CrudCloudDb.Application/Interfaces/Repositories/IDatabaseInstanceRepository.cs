// CrudCloudDb.Application/Interfaces/Repositories/IDatabaseInstanceRepository.cs

using CrudCloudDb.Core.Entities;

namespace CrudCloudDb.Application.Interfaces.Repositories
{
    /// <summary>
    /// Repositorio para operaciones de base de datos con DatabaseInstance
    /// </summary>
    public interface IDatabaseInstanceRepository
    {
        /// <summary>
        /// Obtiene una instancia por su ID
        /// </summary>
        Task<DatabaseInstance?> GetByIdAsync(Guid id);

        /// <summary>
        /// Obtiene una instancia por su Container ID de Docker
        /// </summary>
        Task<DatabaseInstance?> GetByContainerIdAsync(string containerId);

        /// <summary>
        /// Obtiene todas las instancias de un usuario
        /// </summary>
        Task<IEnumerable<DatabaseInstance>> GetByUserIdAsync(Guid userId);

        /// <summary>
        /// Obtiene todas las instancias activas (Running)
        /// </summary>
        Task<IEnumerable<DatabaseInstance>> GetAllActiveAsync();

        /// <summary>
        /// Crea una nueva instancia
        /// </summary>
        Task<DatabaseInstance> CreateAsync(DatabaseInstance databaseInstance);

        /// <summary>
        /// Actualiza una instancia existente
        /// </summary>
        Task UpdateAsync(DatabaseInstance databaseInstance);

        /// <summary>
        /// Elimina una instancia
        /// </summary>
        Task DeleteAsync(Guid id);
    }
}