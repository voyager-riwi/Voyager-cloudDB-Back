// CrudCloudDb.Infrastructure/Services/DockerService.cs
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using CrudCloudDb.Core.Entities;
using CrudCloudDb.Core.Enums;
using CrudCloudDb.Application.Services.Interfaces;

namespace CrudCloudDb.Infrastructure.Services
{
    public class DockerService : IDockerService
    {
        private readonly IDockerClient _dockerClient;
        private readonly IPortManagerService _portManager;
        private readonly ICredentialService _credentialService;
        private readonly ILogger<DockerService> _logger;

        public DockerService(
            IPortManagerService portManager,
            ICredentialService credentialService,
            ILogger<DockerService> logger)
        {
            _portManager = portManager;
            _credentialService = credentialService;
            _logger = logger;

            // Conectar a Docker
            _dockerClient = new DockerClientConfiguration(
                new Uri("unix:///var/run/docker.sock"))  // Linux
                // new Uri("npipe://./pipe/docker_engine"))  // Windows
                .CreateClient();
        }

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

                return dbInstance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error creando contenedor {engine}");
                throw;
            }
        }

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
                    Interval = TimeSpan.FromSeconds(10),
                    Timeout = TimeSpan.FromSeconds(5),
                    Retries = 5,
                    StartPeriod = TimeSpan.FromSeconds(30)
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
                    Interval = TimeSpan.FromSeconds(10),
                    Timeout = TimeSpan.FromSeconds(5),
                    Retries = 5,
                    StartPeriod = TimeSpan.FromSeconds(30)
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
                    Interval = TimeSpan.FromSeconds(10),
                    Timeout = TimeSpan.FromSeconds(5),
                    Retries = 5,
                    StartPeriod = TimeSpan.FromSeconds(30)
                }
            };
        }

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

        // Métodos adicionales...
        public async Task<bool> StopContainerAsync(string containerId)
        {
            try
            {
                await _dockerClient.Containers.StopContainerAsync(containerId, new ContainerStopParameters());
                _logger.LogInformation($"🛑 Contenedor detenido: {containerId[..12]}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deteniendo contenedor");
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

        public async Task<string> GetContainerLogsAsync(string containerId, int lines = 100)
        {
            try
            {
                var logs = await _dockerClient.Containers.GetContainerLogsAsync(
                    containerId,
                    new ContainerLogsParameters
                    {
                        ShowStdout = true,
                        ShowStderr = true,
                        Tail = lines.ToString()
                    });

                using var reader = new StreamReader(logs);
                return await reader.ReadToEndAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo logs");
                return string.Empty;
            }
        }
    }
}
