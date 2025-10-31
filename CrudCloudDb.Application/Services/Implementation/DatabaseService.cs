using CrudCloudDb.Application.Services.Interfaces;
using CrudCloudDb.Application.DTOs.Database;
using CrudCloudDb.Application.DTOs.Email;
using CrudCloudDb.Application.Interfaces.Repositories;
using CrudCloudDb.Core.Entities;
using CrudCloudDb.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

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
        private readonly IConfiguration _configuration;
        private readonly ICredentialService _credentialService;
        private readonly IEmailService _emailService;  // ✅ AGREGADO - Faltaba esto

        public DatabaseService(
            IDockerService dockerService,
            IDatabaseInstanceRepository databaseRepository,
            IUserRepository userRepository,
            ILogger<DatabaseService> logger,
            IConfiguration configuration,
            ICredentialService credentialService,
            IEmailService emailService)  // ✅ AGREGADO - Faltaba esto
        {
            _dockerService = dockerService;
            _databaseRepository = databaseRepository;
            _userRepository = userRepository;
            _logger = logger;
            _configuration = configuration;
            _credentialService = credentialService;
            _emailService = emailService;  // ✅ AGREGADO - Faltaba esto
        }

        /// <summary>
        /// Crea una nueva base de datos
        /// </summary>
        public async Task<DatabaseResponseDto> CreateDatabaseAsync(Guid userId, CreateDatabaseRequestDto request)
        {
            _logger.LogInformation($"📥 Creating {request.Engine} database for user {userId}");

            // 1. Obtener usuario
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogError($"❌ User {userId} not found");
                throw new UnauthorizedAccessException("User not found");
            }

            // 2. Generar nombre aleatorio para la base de datos
            var databaseName = _credentialService.GenerateDatabaseName();
            _logger.LogInformation($"🎲 Generated database name: {databaseName}");

            // 3. Crear base de datos con Docker
            _logger.LogInformation($"🐳 Creating Docker container for {request.Engine}");
            var dbInstance = await _dockerService.CreateDatabaseContainerAsync(
                user,
                request.Engine,
                databaseName);

            // 4. Guardar en BD
            _logger.LogInformation($"💾 Saving database instance to repository");
            await _databaseRepository.CreateAsync(dbInstance);

            _logger.LogInformation($"✅ Database {dbInstance.Name} created successfully");

            // 5. Mapear a DTO y devolver
            return MapToDto(dbInstance, checkRunning: false);
        }

        /// <summary>
        /// Obtiene todas las bases de datos ACTIVAS de un usuario (excluye las eliminadas)
        /// </summary>
        public async Task<List<DatabaseResponseDto>> GetUserDatabasesAsync(Guid userId)
        {
            _logger.LogInformation($"📋 Getting databases for user {userId}");

            var databases = await _databaseRepository.GetByUserIdAsync(userId);

            // 🔒 FILTRAR: Solo mostrar bases de datos NO eliminadas
            var activeDatabases = databases
                .Where(db => db.Status != DatabaseStatus.Deleted)
                .ToList();

            _logger.LogInformation($"✅ Found {activeDatabases.Count} active databases for user {userId}");

            return activeDatabases.Select(db => MapToDto(db, checkRunning: false)).ToList();
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
        /// Marca una base de datos como eliminada (SOFT DELETE - 30 días de gracia)
        /// </summary>
        public async Task<bool> DeleteDatabaseAsync(Guid userId, Guid databaseId)
        {
            _logger.LogInformation($"🗑️ Soft deleting database {databaseId} for user {userId}");

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

            // ⭐ SOFT DELETE: Solo marcar como eliminada, NO eliminar físicamente
            database.Status = DatabaseStatus.Deleted;
            database.DeletedAt = DateTime.UtcNow;

            _logger.LogInformation($"💾 Marking database as deleted (30 days grace period)");
            await _databaseRepository.UpdateAsync(database);

            // ✅ CORREGIDO: Ahora _emailService SÍ existe
            await _emailService.SendDatabaseDeletedEmailAsync(new DatabaseDeletedEmailDto
            {
                UserEmail = user.Email,
                UserName = user.Email.Split('@')[0],
                DatabaseName = database.DatabaseName,
                Engine = database.Engine.ToString(),
                DeletedAt = DateTime.UtcNow
            });

            _logger.LogInformation($"✅ Database {database.Name} marked as deleted (soft delete, will be permanently deleted after 30 days)");

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
        /// Restaura una base de datos marcada como eliminada (dentro del período de gracia de 30 días)
        /// </summary>
        public async Task<bool> RestoreDatabaseAsync(Guid userId, Guid databaseId)
        {
            _logger.LogInformation($"♻️ Restoring database {databaseId} for user {userId}");

            var database = await _databaseRepository.GetByIdAsync(databaseId);

            if (database == null)
            {
                _logger.LogWarning($"⚠️ Database {databaseId} not found");
                return false;
            }

            if (database.UserId != userId)
            {
                _logger.LogWarning($"⚠️ User {userId} tried to restore database {databaseId} owned by another user");
                throw new UnauthorizedAccessException("You don't have access to this database");
            }

            if (database.Status != DatabaseStatus.Deleted)
            {
                _logger.LogWarning($"⚠️ Database {databaseId} is not deleted, cannot restore");
                return false;
            }

            // Verificar que no hayan pasado más de 30 días
            if (database.DeletedAt.HasValue &&
                (DateTime.UtcNow - database.DeletedAt.Value).TotalDays > 30)
            {
                _logger.LogWarning($"⚠️ Cannot restore database {databaseId}: grace period expired (more than 30 days)");
                throw new InvalidOperationException("Cannot restore database: grace period expired (more than 30 days). The database will be permanently deleted soon.");
            }

            // Restaurar la base de datos
            database.Status = DatabaseStatus.Running;
            database.DeletedAt = null;

            _logger.LogInformation($"💾 Restoring database status to Running");
            await _databaseRepository.UpdateAsync(database);

            _logger.LogInformation($"✅ Database {database.Name} restored successfully");

            return true;
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
                Host = GetDatabaseHost(db.Engine),
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