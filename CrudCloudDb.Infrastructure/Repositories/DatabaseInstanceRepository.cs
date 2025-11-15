using CrudCloudDb.Application.Interfaces.Repositories;
using CrudCloudDb.Core.Entities;
using CrudCloudDb.Core.Enums;
using CrudCloudDb.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CrudCloudDb.Infrastructure.Repositories
{
    /// <summary>
    /// Repositorio para gestión de instancias de bases de datos
    /// </summary>
    public class DatabaseInstanceRepository : IDatabaseInstanceRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DatabaseInstanceRepository> _logger;

        public DatabaseInstanceRepository(
            ApplicationDbContext context,
            ILogger<DatabaseInstanceRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Crea una nueva instancia de base de datos
        /// </summary>
        public async Task<DatabaseInstance> CreateAsync(DatabaseInstance databaseInstance)
        {
            try
            {
                _logger.LogInformation($"Creating database instance: {databaseInstance.Name}");
                
                _context.DatabaseInstances.Add(databaseInstance);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"✅ Database instance created with ID: {databaseInstance.Id}");
                
                return databaseInstance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating database instance: {databaseInstance.Name}");
                throw;
            }
        }

        /// <summary>
        /// Obtiene una instancia de base de datos por ID
        /// </summary>
        public async Task<DatabaseInstance?> GetByIdAsync(Guid id)
        {
            try
            {
                _logger.LogInformation($"Getting database instance by ID: {id}");
                
                var database = await _context.DatabaseInstances
                    .FirstOrDefaultAsync(d => d.Id == id);
                
                if (database == null)
                {
                    _logger.LogWarning($"⚠️ Database instance not found: {id}");
                }
                
                return database;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting database instance by ID: {id}");
                throw;
            }
        }

        /// <summary>
        /// Obtiene una instancia de base de datos por container ID
        /// </summary>
        public async Task<DatabaseInstance?> GetByContainerIdAsync(string containerId)
        {
            try
            {
                _logger.LogInformation($"Getting database instance by container ID: {containerId}");
                
                return await _context.DatabaseInstances
                    .FirstOrDefaultAsync(d => d.ContainerId == containerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting database instance by container ID: {containerId}");
                throw;
            }
        }

        /// <summary>
        /// Obtiene todas las bases de datos de un usuario
        /// </summary>
        public async Task<IEnumerable<DatabaseInstance>> GetByUserIdAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation($"Getting database instances for user: {userId}");
                
                return await _context.DatabaseInstances
                    .Where(d => d.UserId == userId)
                    .OrderByDescending(d => d.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting database instances for user: {userId}");
                throw;
            }
        }

        /// <summary>
        /// Obtiene todas las bases de datos activas (Running)
        /// </summary>
        public async Task<IEnumerable<DatabaseInstance>> GetAllActiveAsync()
        {
            try
            {
                _logger.LogInformation("Getting all active database instances");
                
                return await _context.DatabaseInstances
                    .Where(d => d.Status == DatabaseStatus.Running)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all active database instances");
                throw;
            }
        }

        /// <summary>
        /// Actualiza una instancia de base de datos
        /// </summary>
        public async Task UpdateAsync(DatabaseInstance databaseInstance)
        {
            try
            {
                _logger.LogInformation($"Updating database instance: {databaseInstance.Id}");
                
                _context.DatabaseInstances.Update(databaseInstance);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"✅ Database instance updated: {databaseInstance.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating database instance: {databaseInstance.Id}");
                throw;
            }
        }

        /// <summary>
        /// Elimina una instancia de base de datos
        /// </summary>
        public async Task DeleteAsync(Guid id)
        {
            try
            {
                _logger.LogInformation($"Deleting database instance: {id}");
                
                var database = await _context.DatabaseInstances.FindAsync(id);
                
                if (database != null)
                {
                    _context.DatabaseInstances.Remove(database);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"✅ Database instance deleted: {id}");
                }
                else
                {
                    _logger.LogWarning($"⚠️ Database instance not found for deletion: {id}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting database instance: {id}");
                throw;
            }
        }
    }
}