// CrudCloudDb.Infrastructure/Services/DockerService.cs

using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using CrudCloudDb.Core.Entities;
using CrudCloudDb.Core.Enums;
using CrudCloudDb.Application.Services.Interfaces;
using CrudCloudDb.Application.DTOs.Email;

namespace CrudCloudDb.Infrastructure.Services
{
    /// <summary>
    /// Servicio para gestión de contenedores Docker de bases de datos
    /// </summary>
    public class DockerService : IDockerService
    {
        private readonly IDockerClient _dockerClient;
        private readonly IPortManagerService _portManager;
        private readonly ICredentialService _credentialService;
        private readonly IEmailService _emailService;  // ⭐ CORREGIDO: Agregado campo
        private readonly ILogger<DockerService> _logger;

        public DockerService(
            IPortManagerService portManager,
            ICredentialService credentialService,
            IEmailService emailService,
            ILogger<DockerService> logger)
        {
            _portManager = portManager;
            _credentialService = credentialService;
            _emailService = emailService;
            _logger = logger;

            // Conectar a Docker
            _dockerClient = new DockerClientConfiguration(
                new Uri("unix:///var/run/docker.sock"))  // Linux
                // new Uri("npipe://./pipe/docker_engine"))  // Windows
                .CreateClient();
        }

        /// <summary>
        /// Crea un nuevo contenedor de base de datos
        /// </summary>
        public async Task<DatabaseInstance> CreateDatabaseContainerAsync(
            User user,
            DatabaseEngine engine,
            string databaseName)
        {
            try
            {
                _logger.LogInformation($"🚀 [{user.Email}] Creando contenedor {engine}: {databaseName}");

                // 1. OBTENER PUERTO DISPONIBLE
                var port = await _portManager.GetAvailablePortAsync(engine);
                _logger.LogInformation($"📌 Puerto asignado: {port}");

                // 2. GENERAR CREDENCIALES
                var credentials = await _credentialService.GenerateCredentialsAsync();
                _logger.LogInformation($"🔑 Usuario generado: {credentials.Username}");

                // 3. CREAR CONFIGURACIÓN
                var config = engine switch
                {
                    DatabaseEngine.PostgreSQL => CreatePostgreSQLConfig(databaseName, credentials, port),
                    DatabaseEngine.MySQL => CreateMySQLConfig(databaseName, credentials, port),
                    DatabaseEngine.MongoDB => CreateMongoDBConfig(databaseName, credentials, port),
                    _ => throw new NotSupportedException($"Motor {engine} no soportado")
                };

                // 4. ASEGURAR IMAGEN
                await EnsureImageExistsAsync(config.Image);

                // 5. CREAR CONTENEDOR
                _logger.LogInformation($"📦 Creando contenedor Docker...");
                var container = await _dockerClient.Containers.CreateContainerAsync(config);
                var containerId = container.ID[..12];
                _logger.LogInformation($"✅ Contenedor creado: {containerId}");

                // 6. INICIAR CONTENEDOR
                _logger.LogInformation($"▶️  Iniciando contenedor...");
                await _dockerClient.Containers.StartContainerAsync(
                    container.ID,
                    new ContainerStartParameters());

                // 7. ESPERAR HEALTHCHECK
                _logger.LogInformation($"⏳ Esperando healthcheck...");
                var isHealthy = await WaitForContainerHealthyAsync(container.ID, maxWaitSeconds: 90);

                if (!isHealthy)
                {
                    _logger.LogError($"❌ Contenedor no se volvió healthy, eliminando...");
                    await _dockerClient.Containers.StopContainerAsync(container.ID, new ContainerStopParameters());
                    await _dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters());
                    throw new Exception($"Contenedor {containerId} no pasó healthcheck");
                }

                _logger.LogInformation($"✅ Contenedor {containerId} listo!");

                // 8. CREAR INSTANCIA
                var dbInstance = new DatabaseInstance
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Engine = engine,
                    Name = databaseName,
                    ContainerId = container.ID,
                    Port = port,
                    DatabaseName = databaseName,
                    Username = credentials.Username,
                    PasswordHash = credentials.PasswordHash,
                    Status = DatabaseStatus.Running,
                    ConnectionString = BuildConnectionString(engine, port, databaseName, credentials),
                    CredentialsViewed = false,
                    CreatedAt = DateTime.UtcNow
                };

                _logger.LogInformation($"🎉 Base de datos {engine} creada exitosamente en puerto {port}");

                // 9. ENVIAR EMAIL ⭐ CORREGIDO: Usar DTO
                await _emailService.SendDatabaseCreatedEmailAsync(new DatabaseCreatedEmailDto
                {
                    UserEmail = user.Email,
                    UserName = user.Email.Split('@')[0],
                    DatabaseName = databaseName,
                    Engine = engine.ToString(),
                    Username = credentials.Username,
                    Password = credentials.Password,  // ⚠️ Solo aquí en texto plano
                    Port = port,
                    ConnectionString = dbInstance.ConnectionString,
                    CreatedAt = DateTime.UtcNow
                });

                return dbInstance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error creando contenedor {engine}");
                throw;
            }
        }

        /// <summary>
        /// Elimina un contenedor y envía email de notificación
        /// </summary>
        public async Task<bool> DeleteDatabaseAsync(DatabaseInstance dbInstance, User user)
        {
            try
            {
                _logger.LogInformation($"🗑️  Eliminando base de datos {dbInstance.Name}");

                // 1. Detener contenedor
                await StopContainerAsync(dbInstance.ContainerId);

                // 2. Eliminar contenedor
                var removed = await RemoveContainerAsync(dbInstance.ContainerId);

                if (removed)
                {
                    // 3. Enviar email de confirmación
                    await _emailService.SendDatabaseDeletedEmailAsync(new DatabaseDeletedEmailDto
                    {
                        UserEmail = user.Email,
                        UserName = user.Email.Split('@')[0],
                        DatabaseName = dbInstance.Name,
                        Engine = dbInstance.Engine.ToString(),
                        DeletedAt = DateTime.UtcNow
                    });

                    _logger.LogInformation($"✅ Base de datos {dbInstance.Name} eliminada exitosamente");
                }

                return removed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error eliminando base de datos {dbInstance.Name}");
                throw;
            }
        }

        /// <summary>
        /// Resetea la contraseña de una base de datos
        /// </summary>
        public async Task<PasswordResetResult> ResetDatabasePasswordAsync(
            DatabaseInstance dbInstance,
            User user)
        {
            try
            {
                _logger.LogInformation($"🔑 Reseteando password para BD {dbInstance.Name}");

                // 1. Generar nueva contraseña
                var newCredentials = await _credentialService.GenerateCredentialsAsync();

                // 2. Ejecutar comando dentro del contenedor para cambiar password
                var resetSuccess = await ExecutePasswordResetInContainerAsync(
                    dbInstance.ContainerId,
                    dbInstance.Engine,
                    dbInstance.Username,
                    newCredentials.Password
                );

                if (!resetSuccess)
                {
                    throw new Exception("Failed to reset password in container");
                }

                // 3. Construir nuevo connection string
                var newConnectionString = BuildConnectionString(
                    dbInstance.Engine,
                    dbInstance.Port,
                    dbInstance.DatabaseName,
                    new CredentialResult
                    {
                        Username = dbInstance.Username,
                        Password = newCredentials.Password,
                        PasswordHash = newCredentials.PasswordHash
                    }
                );

                // 4. Enviar email con nueva contraseña
                await _emailService.SendPasswordResetEmailAsync(new PasswordResetEmailDto
                {
                    UserEmail = user.Email,
                    UserName = user.Email.Split('@')[0],
                    DatabaseName = dbInstance.Name,
                    Engine = dbInstance.Engine.ToString(),
                    NewUsername = dbInstance.Username,
                    NewPassword = newCredentials.Password,
                    ConnectionString = newConnectionString,
                    ResetAt = DateTime.UtcNow
                });

                _logger.LogInformation($"✅ Password reseteado exitosamente para {dbInstance.Name}");

                return new PasswordResetResult
                {
                    Success = true,
                    NewPassword = newCredentials.Password,
                    NewPasswordHash = newCredentials.PasswordHash,
                    NewConnectionString = newConnectionString
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error reseteando password");
                throw;
            }
        }

        /// <summary>
        /// Rota las credenciales de una base de datos (cambio automático programado)
        /// Similar a reset pero puede ser automático/programado
        /// </summary>
        public async Task<PasswordResetResult> RotateDatabaseCredentialsAsync(
            DatabaseInstance dbInstance,
            User user,
            bool notifyUser = true)
        {
            try
            {
                _logger.LogInformation($"🔄 Rotando credenciales para BD {dbInstance.Name}");

                // 1. Generar nuevas credenciales
                var newCredentials = await _credentialService.GenerateCredentialsAsync();

                // 2. Ejecutar cambio de password en el contenedor
                var resetSuccess = await ExecutePasswordResetInContainerAsync(
                    dbInstance.ContainerId,
                    dbInstance.Engine,
                    dbInstance.Username,
                    newCredentials.Password
                );

                if (!resetSuccess)
                {
                    throw new Exception("Failed to rotate credentials in container");
                }

                // 3. Construir nuevo connection string
                var newConnectionString = BuildConnectionString(
                    dbInstance.Engine,
                    dbInstance.Port,
                    dbInstance.DatabaseName,
                    new CredentialResult
                    {
                        Username = dbInstance.Username,
                        Password = newCredentials.Password,
                        PasswordHash = newCredentials.PasswordHash
                    }
                );

                // 4. Enviar email solo si se solicita (para rotaciones automáticas puede ser opcional)
                if (notifyUser)
                {
                    await _emailService.SendPasswordResetEmailAsync(new PasswordResetEmailDto
                    {
                        UserEmail = user.Email,
                        UserName = user.Email.Split('@')[0],
                        DatabaseName = dbInstance.Name,
                        Engine = dbInstance.Engine.ToString(),
                        NewUsername = dbInstance.Username,
                        NewPassword = newCredentials.Password,
                        ConnectionString = newConnectionString,
                        ResetAt = DateTime.UtcNow
                    });
                }

                _logger.LogInformation($"✅ Credenciales rotadas exitosamente para {dbInstance.Name}");

                return new PasswordResetResult
                {
                    Success = true,
                    NewPassword = newCredentials.Password,
                    NewPasswordHash = newCredentials.PasswordHash,
                    NewConnectionString = newConnectionString
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error rotando credenciales");
                throw;
            }
        }

        // ============================================
        // MÉTODOS DE GESTIÓN DE CONTENEDORES
        // ============================================

        public async Task<bool> StopContainerAsync(string containerId)
        {
            try
            {
                await _dockerClient.Containers.StopContainerAsync(
                    containerId,
                    new ContainerStopParameters { WaitBeforeKillSeconds = 10 });
                
                _logger.LogInformation($"🛑 Contenedor detenido: {containerId[..12]}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deteniendo contenedor");
                return false;
            }
        }

        public async Task<bool> StartContainerAsync(string containerId)
        {
            try
            {
                await _dockerClient.Containers.StartContainerAsync(
                    containerId,
                    new ContainerStartParameters());

                _logger.LogInformation($"▶️  Contenedor iniciado: {containerId[..12]}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error iniciando contenedor");
                return false;
            }
        }

        public async Task<bool> RemoveContainerAsync(string containerId)
        {
            try
            {
                await _dockerClient.Containers.RemoveContainerAsync(
                    containerId,
                    new ContainerRemoveParameters { Force = true });
                
                _logger.LogInformation($"🗑️  Contenedor eliminado: {containerId[..12]}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando contenedor");
                return false;
            }
        }

        public async Task<bool> IsContainerRunningAsync(string containerId)
        {
            try
            {
                var inspect = await _dockerClient.Containers.InspectContainerAsync(containerId);
                return inspect.State.Running;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando estado del contenedor");
                return false;
            }
        }

        public async Task<string> GetContainerLogsAsync(string containerId, int lines = 100)
        {
            try
            {
                #pragma warning disable CS0618 // Suprimir warning de método obsoleto
                var logStream = await _dockerClient.Containers.GetContainerLogsAsync(
                    containerId,
                    new ContainerLogsParameters
                    {
                        ShowStdout = true,
                        ShowStderr = true,
                        Tail = lines.ToString()
                    });
                #pragma warning restore CS0618

                using var reader = new StreamReader(logStream);
                return await reader.ReadToEndAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo logs");
                return string.Empty;
            }
        }
        // ============================================
        // MÉTODOS DE CONFIGURACIÓN POR MOTOR
        // ============================================

        private CreateContainerParameters CreatePostgreSQLConfig(
            string dbName,
            CredentialResult credentials,
            int port)
        {
            var containerName = $"db_postgres_{dbName}_{Guid.NewGuid().ToString("N")[..8]}";

            return new CreateContainerParameters
            {
                Image = "postgres:15-alpine",
                Name = containerName,
                Env = new List<string>
                {
                    $"POSTGRES_DB={dbName}",
                    $"POSTGRES_USER={credentials.Username}",
                    $"POSTGRES_PASSWORD={credentials.Password}"
                },
                HostConfig = new HostConfig
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        {
                            "5432/tcp",
                            new List<PortBinding>
                            {
                                new PortBinding { HostPort = port.ToString() }
                            }
                        }
                    },
                    RestartPolicy = new RestartPolicy
                    {
                        Name = RestartPolicyKind.UnlessStopped
                    }
                },
                Healthcheck = new HealthConfig
                {
                    Test = new[] { "CMD-SHELL", $"pg_isready -U {credentials.Username}" },
                    Interval = TimeSpan.FromSeconds(10),  // ✅ TimeSpan directamente
                    Timeout = TimeSpan.FromSeconds(5),    // ✅ Sin .Ticks
                    Retries = 5,
                    StartPeriod = TimeSpan.FromSeconds(30).Ticks
                }
            };
        }

        private CreateContainerParameters CreateMySQLConfig(
            string dbName,
            CredentialResult credentials,
            int port)
        {
            var containerName = $"db_mysql_{dbName}_{Guid.NewGuid().ToString("N")[..8]}";

            return new CreateContainerParameters
            {
                Image = "mysql:8.0",
                Name = containerName,
                Env = new List<string>
                {
                    $"MYSQL_DATABASE={dbName}",
                    $"MYSQL_USER={credentials.Username}",
                    $"MYSQL_PASSWORD={credentials.Password}",
                    $"MYSQL_ROOT_PASSWORD={Guid.NewGuid()}"
                },
                HostConfig = new HostConfig
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        {
                            "3306/tcp",
                            new List<PortBinding>
                            {
                                new PortBinding { HostPort = port.ToString() }
                            }
                        }
                    },
                    RestartPolicy = new RestartPolicy
                    {
                        Name = RestartPolicyKind.UnlessStopped
                    }
                },
                Healthcheck = new HealthConfig
                {
                    Test = new[] { "CMD", "mysqladmin", "ping", "-h", "localhost" },
                    Interval = TimeSpan.FromSeconds(10),  // ✅ TimeSpan directamente
                    Timeout = TimeSpan.FromSeconds(5),    // ✅ Sin .Ticks
                    Retries = 5,
                    StartPeriod = TimeSpan.FromSeconds(30).Ticks
                }
            };
        }

        private CreateContainerParameters CreateMongoDBConfig(
            string dbName,
            CredentialResult credentials,
            int port)
        {
            var containerName = $"db_mongo_{dbName}_{Guid.NewGuid().ToString("N")[..8]}";

            return new CreateContainerParameters
            {
                Image = "mongo:7",
                Name = containerName,
                Env = new List<string>
                {
                    $"MONGO_INITDB_ROOT_USERNAME={credentials.Username}",
                    $"MONGO_INITDB_ROOT_PASSWORD={credentials.Password}",
                    $"MONGO_INITDB_DATABASE={dbName}"
                },
                HostConfig = new HostConfig
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        {
                            "27017/tcp",
                            new List<PortBinding>
                            {
                                new PortBinding { HostPort = port.ToString() }
                            }
                        }
                    },
                    RestartPolicy = new RestartPolicy
                    {
                        Name = RestartPolicyKind.UnlessStopped
                    }
                },
                Healthcheck = new HealthConfig
                {
                    Test = new[] { "CMD", "mongosh", "--eval", "db.adminCommand('ping')", "--quiet" },
                    Interval = TimeSpan.FromSeconds(10),  // ✅ TimeSpan directamente
                    Timeout = TimeSpan.FromSeconds(5),    // ✅ Sin .Ticks
                    Retries = 5,
                    StartPeriod = TimeSpan.FromSeconds(30).Ticks
                }
            };
        }

        // ============================================
        // MÉTODOS AUXILIARES PRIVADOS
        // ============================================

        private async Task EnsureImageExistsAsync(string image)
        {
            try
            {
                await _dockerClient.Images.InspectImageAsync(image);
                _logger.LogInformation($"✅ Imagen {image} ya existe");
            }
            catch (DockerImageNotFoundException)
            {
                _logger.LogInformation($"📥 Descargando imagen {image}...");
                await _dockerClient.Images.CreateImageAsync(
                    new ImagesCreateParameters { FromImage = image },
                    null,
                    new Progress<JSONMessage>(m =>
                    {
                        if (!string.IsNullOrEmpty(m.Status))
                            _logger.LogInformation($"   {m.Status}");
                    }));
                _logger.LogInformation($"✅ Imagen {image} descargada");
            }
        }

        private async Task<bool> WaitForContainerHealthyAsync(string containerId, int maxWaitSeconds = 60)
        {
            var startTime = DateTime.UtcNow;

            while ((DateTime.UtcNow - startTime).TotalSeconds < maxWaitSeconds)
            {
                var inspect = await _dockerClient.Containers.InspectContainerAsync(containerId);

                var healthStatus = inspect.State.Health?.Status ?? "sin healthcheck";
                var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;

                _logger.LogInformation($"   [{elapsed:F1}s] Estado: {healthStatus}");

                if (healthStatus == "healthy" ||
                    (inspect.State.Running && inspect.State.Health == null))
                {
                    return true;
                }

                if (!inspect.State.Running)
                {
                    _logger.LogError($"   Contenedor no está corriendo");
                    return false;
                }

                await Task.Delay(2000);
            }

            return false;
        }

        private async Task<bool> ExecutePasswordResetInContainerAsync(
            string containerId,
            DatabaseEngine engine,
            string username,
            string newPassword)
        {
            try
            {
                string[] command = engine switch
                {
                    DatabaseEngine.PostgreSQL => new[]
                    {
                        "psql",
                        "-U", username,
                        "-c", $"ALTER USER {username} WITH PASSWORD '{newPassword}';"
                    },

                    DatabaseEngine.MySQL => new[]
                    {
                        "mysql",
                        "-u", "root",
                        "-e", $"ALTER USER '{username}'@'%' IDENTIFIED BY '{newPassword}'; FLUSH PRIVILEGES;"
                    },

                    DatabaseEngine.MongoDB => new[]
                    {
                        "mongosh",
                        "--eval",
                        $"db.updateUser('{username}', {{ pwd: '{newPassword}' }})"
                    },

                    _ => throw new NotSupportedException($"Engine {engine} not supported for password reset")
                };

                var execConfig = new ContainerExecCreateParameters
                {
                    AttachStdout = true,
                    AttachStderr = true,
                    Cmd = command
                };

                var execResponse = await _dockerClient.Exec.ExecCreateContainerAsync(
                    containerId,
                    execConfig
                );

                await _dockerClient.Exec.StartContainerExecAsync(execResponse.ID);

                _logger.LogInformation($"✅ Password reset command executed in container");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error executing password reset command");
                return false;
            }
        }

        private string BuildConnectionString(
            DatabaseEngine engine,
            int port,
            string dbName,
            CredentialResult credentials)
        {
            return engine switch
            {
                DatabaseEngine.PostgreSQL =>
                    $"Host=localhost;Port={port};Database={dbName};Username={credentials.Username};Password={credentials.Password}",

                DatabaseEngine.MySQL =>
                    $"Server=localhost;Port={port};Database={dbName};Uid={credentials.Username};Pwd={credentials.Password}",

                DatabaseEngine.MongoDB =>
                    $"mongodb://{credentials.Username}:{credentials.Password}@localhost:{port}/{dbName}?authSource=admin",

                _ => throw new NotSupportedException()
            };
        }
    }
}