﻿using CrudCloudDb.Application.Services.Interfaces;
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
        private readonly IEmailService _emailService;
        private readonly IWebhookService _webhookService;

        public DatabaseService(
            IDockerService dockerService,
            IDatabaseInstanceRepository databaseRepository,
            IUserRepository userRepository,
            ILogger<DatabaseService> logger,
            IConfiguration configuration,
            ICredentialService credentialService,
            IEmailService emailService,
            IWebhookService webhookService)  
        {
            _dockerService = dockerService;
            _databaseRepository = databaseRepository;
            _userRepository = userRepository;
            _logger = logger;
            _configuration = configuration;
            _credentialService = credentialService;
            _emailService = emailService;
            _webhookService = webhookService;
        }

        /// <summary>
        /// Crea una nueva base de datos
        /// </summary>
        public async Task<DatabaseResponseDto> CreateDatabaseAsync(Guid userId, CreateDatabaseRequestDto request)
        {
            _logger.LogInformation($"📥 Creating {request.Engine} database for user {userId}");

            // 1. Obtener usuario con su plan
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogError($"❌ User {userId} not found");
                throw new UnauthorizedAccessException("User not found");
            }

            if (user.CurrentPlan == null)
            {
                _logger.LogError($"❌ User {userId} has no plan assigned");
                throw new InvalidOperationException("User has no plan assigned");
            }

            // 2. ⭐ VALIDAR LÍMITES DEL PLAN (LÓGICA CORRECTA)
            // Contar TODAS las bases de datos del mismo motor que NO se hayan eliminado permanentemente
            // Esto incluye: Running + Stopped + Deleted (dentro del período de gracia de 30 días)
            var allUserDatabases = (await _databaseRepository.GetByUserIdAsync(userId)).ToList();
            
            // Filtrar solo las del mismo motor que aún existen (no eliminadas permanentemente)
            var databasesForEngine = allUserDatabases
                .Where(db => db.Engine == request.Engine)
                .ToList();
            
            var totalDatabasesForEngine = databasesForEngine.Count;

            // Contar cuántas están activas vs desactivadas (solo para el mensaje)
            var activeDatabases = databasesForEngine
                .Count(db => db.Status != DatabaseStatus.Deleted);
            
            var deletedDatabases = databasesForEngine
                .Count(db => db.Status == DatabaseStatus.Deleted);

            _logger.LogInformation($"📊 User has {totalDatabasesForEngine}/{user.CurrentPlan.DatabaseLimitPerEngine} {request.Engine} databases ({activeDatabases} active, {deletedDatabases} deactivated)");

            // ⭐ VALIDACIÓN: El total (activas + desactivadas) NO debe exceder el límite del plan
            if (totalDatabasesForEngine >= user.CurrentPlan.DatabaseLimitPerEngine)
            {
                _logger.LogWarning($"⚠️ User {userId} has reached the maximum limit for {request.Engine} databases ({user.CurrentPlan.DatabaseLimitPerEngine})");
                
                var errorMessage = $"You have reached the maximum number of {request.Engine} databases allowed in your plan ({user.CurrentPlan.DatabaseLimitPerEngine}). ";
                
                if (deletedDatabases > 0)
                {
                    errorMessage += $"You currently have {activeDatabases} active and {deletedDatabases} deactivated database(s). " +
                                   $"To create a new database, you must either:\n" +
                                   $"1. Restore one of your deactivated databases (will receive new password via email), OR\n" +
                                   $"2. Wait for a deactivated database to be permanently deleted after 30 days, OR\n" +
                                   $"3. Upgrade your plan to get more database slots.";
                }
                else
                {
                    errorMessage += $"You currently have {activeDatabases} active database(s). " +
                                   $"To create a new database, you must either:\n" +
                                   $"1. Deactivate and then wait 30 days for permanent deletion, OR\n" +
                                   $"2. Upgrade your plan to get more database slots.";
                }
                
                throw new InvalidOperationException(errorMessage);
            }

            // 3. Generar nombre aleatorio para la base de datos
            var databaseName = _credentialService.GenerateDatabaseName();
            _logger.LogInformation($"🎲 Generated database name: {databaseName}");

            // 4. Crear base de datos con Docker
            _logger.LogInformation($"🐳 Creating Docker container for {request.Engine}");
            var dbInstance = await _dockerService.CreateDatabaseContainerAsync(
                user,
                request.Engine,
                databaseName);

            // 5. Guardar en BD
            _logger.LogInformation($"Saving database instance to repository");
            await _databaseRepository.CreateAsync(dbInstance);
            var notificationTitle = "Nueva Base de Datos Creada";
            var notificationMessage = 
                $"**Usuario:** {user.Email} ({user.Id})\n" +
                $"**Nombre BD:** {dbInstance.Name}\n" +
                $"**Motor:** {dbInstance.Engine}\n" +
                $"**Estado:** {dbInstance.Status}\n" +
                $"**Fecha:** {DateTime.UtcNow.AddHours(-5):dd/MM/yyyy HH:mm:ss} (UTC-5)";

            await _webhookService.SendSuccesNotificationAsync(notificationTitle, notificationMessage);
            

            _logger.LogInformation($"✅ Database {dbInstance.Name} created successfully ({totalDatabasesForEngine + 1}/{user.CurrentPlan.DatabaseLimitPerEngine})");

            // 6. Mapear a DTO y devolver
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
        /// ⭐ BLOQUEA el acceso cambiando la password temporalmente
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
            if (user == null)
            {
                _logger.LogError($"❌ User {userId} not found");
                throw new KeyNotFoundException("User not found");
            }

            // ⭐ PASO 1: BLOQUEAR ACCESO - Cambiar password a un valor aleatorio
            _logger.LogInformation($"🔒 Blocking access to database by changing password");
            
            // Generar una password temporal que el usuario NO conoce
            var tempPassword = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"); // 64 caracteres aleatorios
            
            var masterContainer = await _dockerService.GetMasterContainerInfoAsync(database.Engine);
            if (masterContainer != null)
            {
                try
                {
                    // Cambiar la password en el motor de base de datos
                    await _dockerService.ResetPasswordInMasterAsync(
                        masterContainer,
                        database.Username,
                        tempPassword,
                        database.Engine);
                    
                    _logger.LogInformation($"✅ Access blocked - password changed to temporary value");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"⚠️ Could not block access by changing password: {ex.Message}");
                }
            }

            // ⭐ PASO 2: Marcar como eliminada
            database.Status = DatabaseStatus.Deleted;
            database.DeletedAt = DateTime.UtcNow;

            _logger.LogInformation($"💾 Marking database as deleted (30 days grace period, access blocked)");
            await _databaseRepository.UpdateAsync(database);
            var notificationTitle = "Base de Datos Eliminada";
            var notificationMessage = $"**Usuario:** {database.User.Id} ({userId})\n" +
                                      $"**Nombre BD:** {database.Name}\n" +
                                      $"**Motor:** {database.Engine}\n" +
                                      $"**ID BD:** {database.Id}";
                              
            // Llamamos al nuevo método de advertencia
            await _webhookService.SendWarningNotificationAsync(notificationTitle, notificationMessage);

            // ⭐ PASO 3: Enviar email de notificación
            await _emailService.SendDatabaseDeletedEmailAsync(new DatabaseDeletedEmailDto
            {
                UserEmail = user.Email,
                UserName = user.Email.Split('@')[0],
                DatabaseName = database.DatabaseName,
                Engine = database.Engine.ToString(),
                DeletedAt = DateTime.UtcNow
            });

            _logger.LogInformation($"✅ Database {database.Name} marked as deleted (access blocked, will be permanently deleted after 30 days)");

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
            if (user == null)
            {
                _logger.LogError($"❌ User {userId} not found");
                throw new KeyNotFoundException("User not found");
            }

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
        /// ⭐ Genera una nueva password y restaura el acceso
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

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogError($"❌ User {userId} not found");
                throw new KeyNotFoundException("User not found");
            }

            // ⭐ PASO 1: Generar nueva password y restaurar acceso
            _logger.LogInformation($"🔑 Generating new password to restore access");
            var newCredentials = await _credentialService.GenerateCredentialsAsync();

            var masterContainer = await _dockerService.GetMasterContainerInfoAsync(database.Engine);
            if (masterContainer != null)
            {
                try
                {
                    // Cambiar la password a la nueva
                    await _dockerService.ResetPasswordInMasterAsync(
                        masterContainer,
                        database.Username,
                        newCredentials.Password,
                        database.Engine);
                    
                    _logger.LogInformation($"✅ Access restored - new password set");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ Failed to restore access by changing password");
                    throw new Exception("Failed to restore database access", ex);
                }
            }

            // ⭐ PASO 2: Actualizar ConnectionString con la nueva password
            var engineName = database.Engine.ToString();
            var envVarName = $"DB_HOST_{engineName.ToUpperInvariant()}";
            var host = Environment.GetEnvironmentVariable(envVarName)
                      ?? _configuration[$"DatabaseHosts:{engineName}"]
                      ?? "localhost";

            var newConnectionString = database.Engine switch
            {
                DatabaseEngine.PostgreSQL =>
                    $"Host={host};Port={database.Port};Database={database.DatabaseName};Username={database.Username};Password={newCredentials.Password}",
                DatabaseEngine.MySQL =>
                    $"Server={host};Port={database.Port};Database={database.DatabaseName};Uid={database.Username};Pwd={newCredentials.Password}",
                DatabaseEngine.MongoDB =>
                    $"mongodb://{database.Username}:{Uri.EscapeDataString(newCredentials.Password)}@{host}:{database.Port}/",
                _ => throw new NotSupportedException()
            };

            // ⭐ PASO 3: Restaurar el estado y actualizar credenciales
            database.Status = DatabaseStatus.Running;
            database.DeletedAt = null;
            database.PasswordHash = newCredentials.PasswordHash;
            database.ConnectionString = newConnectionString;

            _logger.LogInformation($"💾 Restoring database status to Running with new credentials");
            await _databaseRepository.UpdateAsync(database);

            // ⭐ PASO 4: Enviar email con las nuevas credenciales
            await _emailService.SendPasswordResetEmailAsync(new PasswordResetEmailDto
            {
                UserEmail = user.Email,
                UserName = user.Email.Split('@')[0],
                DatabaseName = database.DatabaseName,
                Engine = database.Engine.ToString(),
                NewUsername = database.Username,
                NewPassword = newCredentials.Password,
                ConnectionString = newConnectionString,
                ResetAt = DateTime.UtcNow
            });

            _logger.LogInformation($"✅ Database {database.Name} restored successfully with new password sent to {user.Email}");

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