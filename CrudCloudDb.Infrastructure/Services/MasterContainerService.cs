using CrudCloudDb.Application.Services.Interfaces;
using CrudCloudDb.Core.Enums;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;

namespace CrudCloudDb.Infrastructure.Services
{
    /// <summary>
    /// Servicio para gestionar contenedores maestros compartidos
    /// </summary>
    public class MasterContainerService : IMasterContainerService
    {
        private readonly IDockerClient _dockerClient;
        private readonly ILogger<MasterContainerService> _logger;
        
        // Puertos fijos para contenedores maestros
        private const int POSTGRES_PORT = 5432;
        private const int MYSQL_PORT = 3306;
        private const int MONGODB_PORT = 27017;

        public MasterContainerService(ILogger<MasterContainerService> logger)
        {
            _logger = logger;
            
            // Conectar a Docker
            if (OperatingSystem.IsWindows())
            {
                var endpoints = new[]
                {
                    new Uri("tcp://localhost:2375"),
                    new Uri("npipe://./pipe/docker_engine"),
                };

                foreach (var endpoint in endpoints)
                {
                    try
                    {
                        var config = new DockerClientConfiguration(endpoint);
                        var client = config.CreateClient();
                        client.System.PingAsync().Wait(TimeSpan.FromSeconds(2));
                        _dockerClient = client;
                        _logger.LogInformation("✅ Connected to Docker");
                        break;
                    }
                    catch { }
                }
            }
            else
            {
                _dockerClient = new DockerClientConfiguration(
                    new Uri("unix:///var/run/docker.sock")).CreateClient();
            }
        }

        public async Task<MasterContainerInfo> GetOrCreateMasterContainerAsync(DatabaseEngine engine)
        {
            _logger.LogInformation($"🔍 Getting or creating master container for {engine}");
            
            // Buscar contenedor maestro existente
            var existingContainer = await FindMasterContainerAsync(engine);
            
            if (existingContainer != null)
            {
                _logger.LogInformation($"✅ Found existing master container for {engine}: {existingContainer.ContainerId[..12]}");
                
                // Verificar si está corriendo
                var isRunning = await IsContainerRunningAsync(existingContainer.ContainerId);
                
                if (!isRunning)
                {
                    _logger.LogInformation($"▶️ Starting stopped master container");
                    await _dockerClient.Containers.StartContainerAsync(
                        existingContainer.ContainerId,
                        new ContainerStartParameters());
                    
                    await Task.Delay(3000); // Esperar 3 segundos
                }
                
                existingContainer.IsRunning = true;
                return existingContainer;
            }
            
            // No existe, crear nuevo contenedor maestro
            _logger.LogInformation($"🐳 Creating new master container for {engine}");
            return await CreateMasterContainerAsync(engine);
        }

        public async Task<bool> IsMasterContainerRunningAsync(DatabaseEngine engine)
        {
            var container = await FindMasterContainerAsync(engine);
            
            if (container == null)
                return false;
            
            return await IsContainerRunningAsync(container.ContainerId);
        }

        public async Task<MasterContainerInfo?> GetMasterContainerInfoAsync(DatabaseEngine engine)
        {
            return await FindMasterContainerAsync(engine);
        }

        // ============================================
        // MÉTODOS PRIVADOS
        // ============================================

        private async Task<MasterContainerInfo?> FindMasterContainerAsync(DatabaseEngine engine)
        {
            try
            {
                // Nombres de contenedores existentes en el servidor
                var possibleNames = engine switch
                {
                    DatabaseEngine.PostgreSQL => new[] { "postgres", "voyager_master_postgresql" },
                    DatabaseEngine.MySQL => new[] { "mysql", "voyager_master_mysql" },
                    DatabaseEngine.MongoDB => new[] { "voyager_master_mongodb", "mongo" },
                    _ => Array.Empty<string>()
                };
                
                var containers = await _dockerClient.Containers.ListContainersAsync(
                    new ContainersListParameters { All = true });
                
                foreach (var possibleName in possibleNames)
                {
                    var masterContainer = containers.FirstOrDefault(c => 
                        c.Names.Any(n => n.TrimStart('/') == possibleName));
                    
                    if (masterContainer != null)
                    {
                        var isRunning = masterContainer.State == "running";
                        
                        // Obtener credenciales según el contenedor
                        var (adminUser, adminPassword) = GetCredentialsForEngine(engine);
                        
                        _logger.LogInformation($"✅ Found container: {possibleName} for {engine}");
                        
                        return new MasterContainerInfo
                        {
                            ContainerId = masterContainer.ID,
                            Engine = engine,
                            Port = GetPortForEngine(engine),
                            Host = "172.17.0.1",
                            AdminUsername = adminUser,
                            AdminPassword = adminPassword,
                            IsRunning = isRunning
                        };
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error finding master container for {engine}");
                return null;
            }
        }

        private (string username, string password) GetCredentialsForEngine(DatabaseEngine engine)
        {
            return engine switch
            {
                DatabaseEngine.PostgreSQL => ("postgres", "cambiarestapassword"),
                DatabaseEngine.MySQL => ("root", "cambiarestapassword"),
                DatabaseEngine.MongoDB => ("admin", "SecureMongoPass2024!"),  // ✅ CORRECTA
                _ => throw new NotSupportedException()
            };
        }
        private async Task<MasterContainerInfo> CreateMasterContainerAsync(DatabaseEngine engine)
        {
            var containerName = GetMasterContainerName(engine);
            var port = GetPortForEngine(engine);
            var (adminUser, adminPassword) = GetCredentialsForEngine(engine);
            
            _logger.LogInformation($"📦 Creating master container: {containerName} on port {port}");
            
            // Configuración según el motor
            var config = engine switch
            {
                DatabaseEngine.PostgreSQL => CreatePostgreSQLMasterConfig(containerName, port, adminUser, adminPassword),
                DatabaseEngine.MySQL => CreateMySQLMasterConfig(containerName, port, adminPassword),
                DatabaseEngine.MongoDB => CreateMongoDBMasterConfig(containerName, port, adminUser, adminPassword),
                _ => throw new NotSupportedException($"Engine {engine} not supported")
            };
            
            // Asegurar que la imagen existe
            await EnsureImageExistsAsync(config.Image);
            
            // Crear contenedor
            var container = await _dockerClient.Containers.CreateContainerAsync(config);
            _logger.LogInformation($"✅ Master container created: {container.ID[..12]}");
            
            // Iniciar contenedor
            await _dockerClient.Containers.StartContainerAsync(
                container.ID,
                new ContainerStartParameters());
            
            await Task.Delay(5000); // Esperar 5 segundos
            
            _logger.LogInformation($"🎉 Master container {engine} ready!");
            
            return new MasterContainerInfo
            {
                ContainerId = container.ID,
                Engine = engine,
                Port = port,
                Host = "172.17.0.1",
                AdminUsername = adminUser,
                AdminPassword = adminPassword,
                IsRunning = true
            };
        }

        private CreateContainerParameters CreatePostgreSQLMasterConfig(string name, int port, string adminUser, string adminPassword)
        {
            return new CreateContainerParameters
            {
                Image = "postgres:15-alpine",
                Name = name,
                Env = new List<string>
                {
                    $"POSTGRES_USER={adminUser}",
                    $"POSTGRES_PASSWORD={adminPassword}",
                    "POSTGRES_DB=postgres"
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
                }
            };
        }

        private CreateContainerParameters CreateMySQLMasterConfig(string name, int port, string adminPassword)
        {
            return new CreateContainerParameters
            {
                Image = "mysql:8.0",
                Name = name,
                Env = new List<string>
                {
                    $"MYSQL_ROOT_PASSWORD={adminPassword}"
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
                }
            };
        }

        private CreateContainerParameters CreateMongoDBMasterConfig(string name, int port, string adminUser, string adminPassword)
        {
            return new CreateContainerParameters
            {
                Image = "mongo:7",
                Name = name,
                Env = new List<string>
                {
                    $"MONGO_INITDB_ROOT_USERNAME={adminUser}",
                    $"MONGO_INITDB_ROOT_PASSWORD={adminPassword}"
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
                }
            };
        }

        private string GetMasterContainerName(DatabaseEngine engine)
        {
            return $"voyager_master_{engine.ToString().ToLower()}";
        }

        private int GetPortForEngine(DatabaseEngine engine)
        {
            return engine switch
            {
                DatabaseEngine.PostgreSQL => POSTGRES_PORT,
                DatabaseEngine.MySQL => MYSQL_PORT,
                DatabaseEngine.MongoDB => MONGODB_PORT,
                _ => throw new NotSupportedException()
            };
        }

        private async Task<bool> IsContainerRunningAsync(string containerId)
        {
            try
            {
                var inspect = await _dockerClient.Containers.InspectContainerAsync(containerId);
                return inspect.State.Running;
            }
            catch
            {
                return false;
            }
        }

        private async Task EnsureImageExistsAsync(string image)
        {
            try
            {
                await _dockerClient.Images.InspectImageAsync(image);
                _logger.LogInformation($"✅ Image {image} already exists");
            }
            catch (DockerImageNotFoundException)
            {
                _logger.LogInformation($"📥 Downloading image {image}...");
                await _dockerClient.Images.CreateImageAsync(
                    new ImagesCreateParameters { FromImage = image },
                    null,
                    new Progress<JSONMessage>());
                _logger.LogInformation($"✅ Image {image} downloaded");
            }
        }
    }
}