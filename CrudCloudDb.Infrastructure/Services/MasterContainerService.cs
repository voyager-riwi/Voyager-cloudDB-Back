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
        private const int SQLSERVER_PORT = 1433;

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
            _logger.LogInformation($"🔍 Getting master container info for {engine}");
            
            // En producción, los contenedores maestros YA EXISTEN en el servidor remoto
            // NO intentamos crearlos, solo devolvemos la información
            var masterInfo = GetRemoteMasterContainerInfo(engine);
            
            _logger.LogInformation($"✅ Using remote master container for {engine} on {masterInfo.Host}:{masterInfo.Port}");
            
            return await Task.FromResult(masterInfo);
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
            // Devolver información de los contenedores maestros remotos
            return await Task.FromResult(GetRemoteMasterContainerInfo(engine));
        }

        // ============================================
        // MÉTODOS PRIVADOS
        // ============================================

        /// <summary>
        /// Devuelve la información de los contenedores maestros remotos que YA EXISTEN en el servidor
        /// </summary>
        private MasterContainerInfo GetRemoteMasterContainerInfo(DatabaseEngine engine)
        {
            var (adminUser, adminPassword) = GetCredentialsForEngine(engine);
            var port = GetPortForEngine(engine);
            
            // ⭐ Host remoto: 91.98.42.248 (servidor donde están los contenedores maestros)
            var remoteHost = "91.98.42.248";
            
            return new MasterContainerInfo
            {
                ContainerId = $"remote-{engine.ToString().ToLower()}", // ID ficticio, no lo usamos para conexiones remotas
                Engine = engine,
                Port = port,
                Host = remoteHost, // ⭐ SERVIDOR REMOTO
                AdminUsername = adminUser,
                AdminPassword = adminPassword,
                IsRunning = true // Asumimos que están corriendo
            };
        }

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
                    DatabaseEngine.SQLServer => new[] { "sqlserver", "voyager_master_sqlserver" },
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
                            Host = "172.17.0.1", // Gateway de Docker - permite que contenedores accedan al host
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
                DatabaseEngine.SQLServer => ("sa", "YourStrong!Passw0rd"),
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
                DatabaseEngine.SQLServer => CreateSQLServerMasterConfig(containerName, port, adminPassword),
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
                Host = "172.17.0.1", // Gateway de Docker - permite que contenedores accedan al host
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

        private CreateContainerParameters CreateSQLServerMasterConfig(string name, int port, string adminPassword)
        {
            return new CreateContainerParameters
            {
                Image = "mcr.microsoft.com/mssql/server:2022-latest",
                Name = name,
                Env = new List<string>
                {
                    "ACCEPT_EULA=Y",  // ⭐ Obligatorio para SQL Server
                    $"SA_PASSWORD={adminPassword}",
                    "MSSQL_PID=Express"  // Usar SQL Server Express (gratuito)
                },
                HostConfig = new HostConfig
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        {
                            "1433/tcp",
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
                DatabaseEngine.SQLServer => SQLSERVER_PORT,
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