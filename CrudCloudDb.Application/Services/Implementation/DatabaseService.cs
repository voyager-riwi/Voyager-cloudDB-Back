using CrudCloudDb.Application.Services.Interfaces;
using CrudCloudDb.Application.DTOs.Database;
using CrudCloudDb.Application.Interfaces.Repositories;
using CrudCloudDb.Core.Entities;
using CrudCloudDb.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration; // ⭐ NUEVA LÍNEA

namespace CrudCloudDb.Application.Services.Implementation
{
    /// <summary>
    /// Servicio de lógica de negocio para gestión de bases de datos
    /// </summary>
    public class DatabaseService : IDatabaseService
    {
        private readonly IDockerService _dockerService;
        private readonly IDatabaseInstanceRepository _databaseRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<DatabaseService> _logger;
        private readonly IConfiguration _configuration; // ⭐ NUEVA LÍNEA

        public DatabaseService(
            IDockerService dockerService,
            IDatabaseInstanceRepository databaseRepository,
            IUserRepository userRepository,
            ILogger<DatabaseService> logger,
            IConfiguration configuration) // ⭐ NUEVO PARÁMETRO
        {
            _dockerService = dockerService;
            _databaseRepository = databaseRepository;
            _userRepository = userRepository;
            _logger = logger;
            _configuration = configuration; // ⭐ NUEVA LÍNEA
        }

        /// <summary>
        /// Crea una nueva base de datos
        /// </summary>
        public async Task<DatabaseResponseDto> CreateDatabaseAsync(Guid userId, CreateDatabaseRequestDto request)
        {
            _logger.LogInformation($"📥 Creating {request.Engine} database '{request.DatabaseName}' for user {userId}");

            // 1. Obtener usuario
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogError($"❌ User {userId} not found");
                throw new UnauthorizedAccessException("User not found");
            }

            // 2. Crear base de datos con Docker
            _logger.LogInformation($"🐳 Creating Docker container for {request.Engine}");
            var dbInstance = await _dockerService.CreateDatabaseContainerAsync(
                user,
                request.Engine,
                request.DatabaseName
            );

            // 3. Guardar en BD
            _logger.LogInformation($"💾 Saving database instance to repository");
            await _databaseRepository.CreateAsync(dbInstance);

            _logger.LogInformation($"✅ Database {dbInstance.Name} created successfully");

            // 4. Mapear a DTO y devolver
            return MapToDto(dbInstance, checkRunning: false);
        }

        /// <summary>
        /// Obtiene todas las bases de datos de un usuario
        /// </summary>
        public async Task<List<DatabaseResponseDto>> GetUserDatabasesAsync(Guid userId)
        {
            _logger.LogInformation($"📋 Getting databases for user {userId}");

            var databases = await _databaseRepository.GetByUserIdAsync(userId);
            var databasesList = databases.ToList();

            _logger.LogInformation($"✅ Found {databasesList.Count} databases for user {userId}");

            return databasesList.Select(db => MapToDto(db, checkRunning: false)).ToList();
        }

        /// <summary>
        /// Obtiene los detalles de una base de datos específica
        /// </summary>
        public async Task<DatabaseResponseDto?> GetDatabaseByIdAsync(Guid userId, Guid databaseId)
        {
            _logger.LogInformation($"🔍 Getting database {databaseId} for user {userId}");

            var database = await _databaseRepository.GetByIdAsync(databaseId);

            if (database == null)
            {
                _logger.LogWarning($"⚠️ Database {databaseId} not found");
                return null;
            }

            if (database.UserId != userId)
            {
                _logger.LogWarning($"⚠️ User {userId} tried to access database {databaseId} owned by another user");
                throw new UnauthorizedAccessException("You don't have access to this database");
            }

            // Verificar estado del contenedor
            var isRunning = await _dockerService.IsContainerRunningAsync(database.ContainerId);

            // Actualizar estado si cambió
            if (isRunning && database.Status != DatabaseStatus.Running)
            {
                _logger.LogInformation($"🔄 Updating database {databaseId} status to Running");
                database.Status = DatabaseStatus.Running;
                await _databaseRepository.UpdateAsync(database);
            }
            else if (!isRunning && database.Status == DatabaseStatus.Running)
            {
                _logger.LogInformation($"🔄 Updating database {databaseId} status to Stopped");
                database.Status = DatabaseStatus.Stopped;
                await _databaseRepository.UpdateAsync(database);
            }

            return MapToDto(database, isRunning: isRunning);
        }

        /// <summary>
        /// Elimina una base de datos permanentemente
        /// </summary>
        public async Task<bool> DeleteDatabaseAsync(Guid userId, Guid databaseId)
        {
            _logger.LogInformation($"🗑️ Deleting database {databaseId} for user {userId}");

            var database = await _databaseRepository.GetByIdAsync(databaseId);

            if (database == null)
            {
                _logger.LogWarning($"⚠️ Database {databaseId} not found");
                return false;
            }

            if (database.UserId != userId)
            {
                _logger.LogWarning($"⚠️ User {userId} tried to delete database {databaseId} owned by another user");
                throw new UnauthorizedAccessException("You don't have access to this database");
            }

            var user = await _userRepository.GetByIdAsync(userId);

            // Eliminar contenedor Docker
            _logger.LogInformation($"🐳 Deleting Docker container {database.ContainerId}");
            await _dockerService.DeleteDatabaseAsync(database, user);

            // Eliminar de BD
            _logger.LogInformation($"💾 Deleting database from repository");
            await _databaseRepository.DeleteAsync(databaseId);

            _logger.LogInformation($"✅ Database {database.Name} deleted successfully");

            return true;
        }

        /// <summary>
        /// Resetea el password de una base de datos
        /// </summary>
        public async Task ResetPasswordAsync(Guid userId, Guid databaseId)
        {
            _logger.LogInformation($"🔑 Resetting password for database {databaseId}");

            var database = await _databaseRepository.GetByIdAsync(databaseId);

            if (database == null)
            {
                _logger.LogError($"❌ Database {databaseId} not found");
                throw new KeyNotFoundException("Database not found");
            }

            if (database.UserId != userId)
            {
                _logger.LogWarning($"⚠️ User {userId} tried to reset password for database {databaseId} owned by another user");
                throw new UnauthorizedAccessException("You don't have access to this database");
            }

            var user = await _userRepository.GetByIdAsync(userId);

            // Resetear password en Docker
            _logger.LogInformation($"🐳 Resetting password in Docker container");
            var result = await _dockerService.ResetDatabasePasswordAsync(database, user);

            if (!result.Success)
            {
                _logger.LogError($"❌ Failed to reset password for database {databaseId}");
                throw new Exception("Failed to reset password");
            }

            // Actualizar BD
            _logger.LogInformation($"💾 Updating password hash in repository");
            database.PasswordHash = result.NewPasswordHash;
            database.ConnectionString = result.NewConnectionString;
            await _databaseRepository.UpdateAsync(database);

            _logger.LogInformation($"✅ Password reset successfully for database {database.Name}");
        }

        /// <summary>
        /// Inicia un contenedor detenido
        /// </summary>
        public async Task<bool> StartDatabaseAsync(Guid userId, Guid databaseId)
        {
            _logger.LogInformation($"▶️ Starting database {databaseId}");

            var database = await _databaseRepository.GetByIdAsync(databaseId);

            if (database == null || database.UserId != userId)
            {
                _logger.LogWarning($"⚠️ Database {databaseId} not found or access denied");
                return false;
            }

            var started = await _dockerService.StartContainerAsync(database.ContainerId);

            if (started)
            {
                _logger.LogInformation($"🔄 Updating database {databaseId} status to Running");
                database.Status = DatabaseStatus.Running;
                await _databaseRepository.UpdateAsync(database);
                _logger.LogInformation($"✅ Database {database.Name} started successfully");
            }
            else
            {
                _logger.LogError($"❌ Failed to start database {databaseId}");
            }

            return started;
        }

        /// <summary>
        /// Detiene un contenedor en ejecución
        /// </summary>
        public async Task<bool> StopDatabaseAsync(Guid userId, Guid databaseId)
        {
            _logger.LogInformation($"⏸️ Stopping database {databaseId}");

            var database = await _databaseRepository.GetByIdAsync(databaseId);

            if (database == null || database.UserId != userId)
            {
                _logger.LogWarning($"⚠️ Database {databaseId} not found or access denied");
                return false;
            }

            var stopped = await _dockerService.StopContainerAsync(database.ContainerId);

            if (stopped)
            {
                _logger.LogInformation($"🔄 Updating database {databaseId} status to Stopped");
                database.Status = DatabaseStatus.Stopped;
                await _databaseRepository.UpdateAsync(database);
                _logger.LogInformation($"✅ Database {database.Name} stopped successfully");
            }
            else
            {
                _logger.LogError($"❌ Failed to stop database {databaseId}");
            }

            return stopped;
        }

        // ============================================
        // MÉTODOS PRIVADOS
        // ============================================
        
        /// <summary>
        /// Obtiene el host configurado según el motor de base de datos
        /// </summary>
        private string GetDatabaseHost(DatabaseEngine engine)
        {
            var engineName = engine.ToString();
            var host = _configuration[$"DatabaseHosts:{engineName}"];
            
            if (string.IsNullOrEmpty(host))
            {
                _logger.LogWarning($"⚠️ Host not configured for {engineName}, using localhost");
                return "localhost";
            }
            
            _logger.LogInformation($"🌐 Using host {host} for {engineName}");
            return host;
        }

        /// <summary>
        /// Mapea una entidad DatabaseInstance a DTO
        /// </summary>
        private DatabaseResponseDto MapToDto(DatabaseInstance db, bool? isRunning = null, bool checkRunning = true)
        {
            return new DatabaseResponseDto
            {
                Id = db.Id,
                Name = db.Name,
                Engine = db.Engine.ToString(),
                Status = db.Status.ToString(),
                Port = db.Port,
                Host = GetDatabaseHost(db.Engine), // ⭐ CAMBIÓ DE "localhost" A GetDatabaseHost
                Username = db.Username,
                ConnectionString = db.ConnectionString,
                CreatedAt = db.CreatedAt,
                CredentialsViewed = db.CredentialsViewed,
                ContainerId = db.ContainerId.Length >= 12 ? db.ContainerId[..12] : db.ContainerId,
                IsRunning = isRunning ?? (checkRunning ? db.Status == DatabaseStatus.Running : false)
            };
        }
    }
}