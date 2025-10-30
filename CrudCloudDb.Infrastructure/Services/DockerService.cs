using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using CrudCloudDb.Core.Entities;
using CrudCloudDb.Core.Enums;
using CrudCloudDb.Application.Services.Interfaces;
using CrudCloudDb.Application.DTOs.Email;
using CrudCloudDb.Application.DTOs.Credential;
using Npgsql;
using MySqlConnector;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;

namespace CrudCloudDb.Infrastructure.Services
{
    public class DockerService : IDockerService
    {
        private readonly IDockerClient _dockerClient;
        private readonly IMasterContainerService _masterContainerService;
        private readonly ICredentialService _credentialService;
        private readonly IEmailService _emailService;
        private readonly ILogger<DockerService> _logger;
        private readonly IConfiguration _configuration;

        public DockerService(
            IMasterContainerService masterContainerService,
            ICredentialService credentialService,
            IEmailService emailService,
            ILogger<DockerService> logger,
            IConfiguration configuration)
        {
            _masterContainerService = masterContainerService;
            _credentialService = credentialService;
            _emailService = emailService;
            _logger = logger;
            _configuration = configuration;

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

        public async Task<DatabaseInstance> CreateDatabaseContainerAsync(
            User user,
            DatabaseEngine engine,
            string databaseName)
        {
            try
            {
                _logger.LogInformation($"🚀 [{user.Email}] Creating {engine} database: {databaseName}");

                var masterContainer = await _masterContainerService.GetOrCreateMasterContainerAsync(engine);
                _logger.LogInformation($"📦 Using master container: {masterContainer.ContainerId[..12]} on port {masterContainer.Port}");

                var credentials = await _credentialService.GenerateCredentialsAsync();
                _logger.LogInformation($"🔑 Generated credentials for user: {credentials.Username}");

                await CreateDatabaseInsideMasterAsync(
                    masterContainer,
                    databaseName,
                    credentials,
                    engine);

                _logger.LogInformation($"✅ Database {databaseName} created inside master container");

                var dbInstance = new DatabaseInstance
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Engine = engine,
                    Name = databaseName,
                    MasterContainerId = masterContainer.ContainerId,
                    Port = masterContainer.Port,
                    DatabaseName = databaseName,
                    Username = credentials.Username,
                    PasswordHash = credentials.PasswordHash,
                    Status = DatabaseStatus.Running,
                    ConnectionString = BuildConnectionString(
                        engine,
                        masterContainer.Port,
                        databaseName,
                        credentials),
                    CredentialsViewed = false,
                    CreatedAt = DateTime.UtcNow
                };

                _logger.LogInformation($"🎉 Database {engine}/{databaseName} ready on port {masterContainer.Port}");

                await _emailService.SendDatabaseCreatedEmailAsync(new DatabaseCreatedEmailDto
                {
                    UserEmail = user.Email,
                    UserName = user.Email.Split('@')[0],
                    DatabaseName = databaseName,
                    Engine = engine.ToString(),
                    Username = credentials.Username,
                    Password = credentials.Password,
                    Port = masterContainer.Port,
                    ConnectionString = dbInstance.ConnectionString,
                    CreatedAt = DateTime.UtcNow
                });

                return dbInstance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error creating database {engine}/{databaseName}");
                throw;
            }
        }

        public async Task<bool> DeleteDatabaseAsync(DatabaseInstance dbInstance, User user)
        {
            try
            {
                _logger.LogInformation($"🗑️ Deleting database {dbInstance.Name}");

                var masterContainer = await _masterContainerService.GetMasterContainerInfoAsync(dbInstance.Engine);

                if (masterContainer == null)
                {
                    _logger.LogWarning($"⚠️ Master container not found for {dbInstance.Engine}");
                    return false;
                }

                await DeleteDatabaseInsideMasterAsync(
                    masterContainer,
                    dbInstance.DatabaseName,
                    dbInstance.Username,
                    dbInstance.Engine);

                await _emailService.SendDatabaseDeletedEmailAsync(new DatabaseDeletedEmailDto
                {
                    UserEmail = user.Email,
                    UserName = user.Email.Split('@')[0],
                    DatabaseName = dbInstance.Name,
                    Engine = dbInstance.Engine.ToString(),
                    DeletedAt = DateTime.UtcNow
                });

                _logger.LogInformation($"✅ Database {dbInstance.Name} deleted successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error deleting database {dbInstance.Name}");
                throw;
            }
        }

        public async Task<PasswordResetResult> ResetDatabasePasswordAsync(
            DatabaseInstance dbInstance,
            User user)
        {
            try
            {
                _logger.LogInformation($"🔑 Resetting password for database {dbInstance.Name}");

                var masterContainer = await _masterContainerService.GetMasterContainerInfoAsync(dbInstance.Engine);

                if (masterContainer == null)
                    throw new Exception("Master container not found");

                var newCredentials = await _credentialService.GenerateCredentialsAsync();

                await ResetPasswordInsideMasterAsync(
                    masterContainer,
                    dbInstance.Username,
                    newCredentials.Password,
                    dbInstance.Engine);

                var newConnectionString = BuildConnectionString(
                    dbInstance.Engine,
                    masterContainer.Port,
                    dbInstance.DatabaseName,
                    newCredentials);

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

                _logger.LogInformation($"✅ Password reset successfully for {dbInstance.Name}");

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
                _logger.LogError(ex, $"❌ Error resetting password");
                throw;
            }
        }

        public async Task<bool> IsContainerRunningAsync(string containerId)
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

        public async Task<bool> StartContainerAsync(string containerId)
        {
            try
            {
                await _dockerClient.Containers.StartContainerAsync(
                    containerId,
                    new ContainerStartParameters());
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting container");
                return false;
            }
        }

        public async Task<bool> StopContainerAsync(string containerId)
        {
            try
            {
                await _dockerClient.Containers.StopContainerAsync(
                    containerId,
                    new ContainerStopParameters { WaitBeforeKillSeconds = 10 });
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping container");
                return false;
            }
        }

        public Task<bool> RemoveContainerAsync(string containerId)
        {
            _logger.LogWarning("⚠️ RemoveContainerAsync called but containers are shared - ignoring");
            return Task.FromResult(true);
        }

        public async Task<string> GetContainerLogsAsync(string containerId, int lines = 100)
        {
            try
            {
#pragma warning disable CS0618
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
                _logger.LogError(ex, "Error getting logs");
                return string.Empty;
            }
        }

        // ============================================
        // MÉTODOS PRIVADOS
        // ============================================

        private async Task CreateDatabaseInsideMasterAsync(
            MasterContainerInfo masterContainer,
            string databaseName,
            CredentialsResult credentials,
            DatabaseEngine engine)
        {
            switch (engine)
            {
                case DatabaseEngine.PostgreSQL:
                    await CreatePostgreSQLDatabaseAsync(masterContainer, databaseName, credentials);
                    break;

                case DatabaseEngine.MySQL:
                    await CreateMySQLDatabaseAsync(masterContainer, databaseName, credentials);
                    break;

                case DatabaseEngine.MongoDB:
                    await CreateMongoDBDatabaseAsync(masterContainer, databaseName, credentials);
                    break;

                default:
                    throw new NotSupportedException($"Engine {engine} not supported");
            }
        }

        private async Task CreatePostgreSQLDatabaseAsync(
            MasterContainerInfo master,
            string dbName,
            CredentialsResult credentials)
        {
            var connString = $"Host={master.Host};Port={master.Port};Database=postgres;Username={master.AdminUsername};Password={master.AdminPassword}";

            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();

            // Crear usuario
            await using (var cmd = new NpgsqlCommand(
                $"CREATE USER {credentials.Username} WITH PASSWORD '{credentials.Password}'", conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Crear base de datos
            await using (var cmd = new NpgsqlCommand(
                $"CREATE DATABASE {dbName} OWNER {master.AdminUsername}", conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Dar permiso CONNECT
            await using (var cmd = new NpgsqlCommand(
                $"GRANT CONNECT ON DATABASE {dbName} TO {credentials.Username}", conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Cerrar conexión a postgres
            await conn.CloseAsync();

            // Abrir nueva conexión a la base de datos recién creada
            var newConnString = $"Host={master.Host};Port={master.Port};Database={dbName};Username={master.AdminUsername};Password={master.AdminPassword}";
            await using var newConn = new NpgsqlConnection(newConnString);
            await newConn.OpenAsync();

            // Dar privilegios en el schema public
            await using (var cmd = new NpgsqlCommand(
                $"GRANT ALL PRIVILEGES ON SCHEMA public TO {credentials.Username}", newConn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Dar privilegios por defecto en tablas futuras
            await using (var cmd = new NpgsqlCommand(
                $"ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO {credentials.Username}", newConn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            _logger.LogInformation($"✅ PostgreSQL database {dbName} created with secure permissions");
        }

        private async Task CreateMySQLDatabaseAsync(
            MasterContainerInfo master,
            string dbName,
            CredentialsResult credentials)
        {
            var connString = $"Server={master.Host};Port={master.Port};User={master.AdminUsername};Password={master.AdminPassword}";

            await using var conn = new MySqlConnection(connString);
            await conn.OpenAsync();

            await using (var cmd = new MySqlCommand($"CREATE DATABASE {dbName}", conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            await using (var cmd = new MySqlCommand(
                $"CREATE USER '{credentials.Username}'@'%' IDENTIFIED BY '{credentials.Password}'", conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            await using (var cmd = new MySqlCommand(
                $"GRANT ALL PRIVILEGES ON {dbName}.* TO '{credentials.Username}'@'%'", conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            await using (var cmd = new MySqlCommand("FLUSH PRIVILEGES", conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            _logger.LogInformation($"✅ MySQL database {dbName} created with secure permissions");
        }

        private async Task CreateMongoDBDatabaseAsync(
            MasterContainerInfo master,
            string dbName,
            CredentialsResult credentials)
        {
            var connString = $"mongodb://{master.AdminUsername}:{master.AdminPassword}@{master.Host}:{master.Port}/admin";

            var client = new MongoClient(connString);
            var adminDb = client.GetDatabase("admin");

            var command = new MongoDB.Bson.BsonDocument
            {
                { "createUser", credentials.Username },
                { "pwd", credentials.Password },
                { "roles", new MongoDB.Bson.BsonArray
                    {
                        new MongoDB.Bson.BsonDocument
                        {
                            { "role", "dbOwner" },
                            { "db", dbName }
                        }
                    }
                }
            };

            await adminDb.RunCommandAsync<MongoDB.Bson.BsonDocument>(command);

            _logger.LogInformation($"✅ MongoDB database {dbName} created with secure permissions");
        }

        private async Task DeleteDatabaseInsideMasterAsync(
            MasterContainerInfo master,
            string dbName,
            string username,
            DatabaseEngine engine)
        {
            switch (engine)
            {
                case DatabaseEngine.PostgreSQL:
                    await DeletePostgreSQLDatabaseAsync(master, dbName, username);
                    break;

                case DatabaseEngine.MySQL:
                    await DeleteMySQLDatabaseAsync(master, dbName, username);
                    break;

                case DatabaseEngine.MongoDB:
                    await DeleteMongoDBDatabaseAsync(master, dbName, username);
                    break;
            }
        }

        private async Task DeletePostgreSQLDatabaseAsync(
            MasterContainerInfo master,
            string dbName,
            string username)
        {
            var connString = $"Host={master.Host};Port={master.Port};Database=postgres;Username={master.AdminUsername};Password={master.AdminPassword}";

            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();

            await using (var cmd = new NpgsqlCommand(
                $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{dbName}'", conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            await using (var cmd = new NpgsqlCommand($"DROP DATABASE IF EXISTS {dbName}", conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            await using (var cmd = new NpgsqlCommand($"DROP USER IF EXISTS {username}", conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            _logger.LogInformation($"✅ PostgreSQL database {dbName} deleted");
        }

        private async Task DeleteMySQLDatabaseAsync(
            MasterContainerInfo master,
            string dbName,
            string username)
        {
            var connString = $"Server={master.Host};Port={master.Port};User={master.AdminUsername};Password={master.AdminPassword}";

            await using var conn = new MySqlConnection(connString);
            await conn.OpenAsync();

            await using (var cmd = new MySqlCommand($"DROP DATABASE IF EXISTS {dbName}", conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            await using (var cmd = new MySqlCommand($"DROP USER IF EXISTS '{username}'@'%'", conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            await using (var cmd = new MySqlCommand("FLUSH PRIVILEGES", conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            _logger.LogInformation($"✅ MySQL database {dbName} deleted");
        }

        private async Task DeleteMongoDBDatabaseAsync(
            MasterContainerInfo master,
            string dbName,
            string username)
        {
            var connString = $"mongodb://{master.AdminUsername}:{master.AdminPassword}@{master.Host}:{master.Port}/admin";

            var client = new MongoClient(connString);

            await client.DropDatabaseAsync(dbName);

            var adminDb = client.GetDatabase("admin");
            var command = new MongoDB.Bson.BsonDocument
            {
                { "dropUser", username }
            };
            await adminDb.RunCommandAsync<MongoDB.Bson.BsonDocument>(command);

            _logger.LogInformation($"✅ MongoDB database {dbName} deleted");
        }

        private async Task ResetPasswordInsideMasterAsync(
            MasterContainerInfo master,
            string username,
            string newPassword,
            DatabaseEngine engine)
        {
            switch (engine)
            {
                case DatabaseEngine.PostgreSQL:
                    await ResetPostgreSQLPasswordAsync(master, username, newPassword);
                    break;

                case DatabaseEngine.MySQL:
                    await ResetMySQLPasswordAsync(master, username, newPassword);
                    break;

                case DatabaseEngine.MongoDB:
                    await ResetMongoDBPasswordAsync(master, username, newPassword);
                    break;
            }
        }

        private async Task ResetPostgreSQLPasswordAsync(
            MasterContainerInfo master,
            string username,
            string newPassword)
        {
            var connString = $"Host={master.Host};Port={master.Port};Database=postgres;Username={master.AdminUsername};Password={master.AdminPassword}";

            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(
                $"ALTER USER {username} WITH PASSWORD '{newPassword}'", conn);
            await cmd.ExecuteNonQueryAsync();

            _logger.LogInformation($"✅ PostgreSQL password reset for {username}");
        }

        private async Task ResetMySQLPasswordAsync(
            MasterContainerInfo master,
            string username,
            string newPassword)
        {
            var connString = $"Server={master.Host};Port={master.Port};User={master.AdminUsername};Password={master.AdminPassword}";

            await using var conn = new MySqlConnection(connString);
            await conn.OpenAsync();

            await using (var cmd = new MySqlCommand(
                $"ALTER USER '{username}'@'%' IDENTIFIED BY '{newPassword}'", conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            await using (var cmd = new MySqlCommand("FLUSH PRIVILEGES", conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            _logger.LogInformation($"✅ MySQL password reset for {username}");
        }

        private async Task ResetMongoDBPasswordAsync(
            MasterContainerInfo master,
            string username,
            string newPassword)
        {
            var connString = $"mongodb://{master.AdminUsername}:{master.AdminPassword}@{master.Host}:{master.Port}/admin";

            var client = new MongoClient(connString);
            var adminDb = client.GetDatabase("admin");

            var command = new MongoDB.Bson.BsonDocument
            {
                { "updateUser", username },
                { "pwd", newPassword }
            };

            await adminDb.RunCommandAsync<MongoDB.Bson.BsonDocument>(command);

            _logger.LogInformation($"✅ MongoDB password reset for {username}");
        }

        private string BuildConnectionString(
            DatabaseEngine engine,
            int port,
            string dbName,
            CredentialsResult credentials)
        {
            // Obtener host desde configuración
            var engineName = engine.ToString();
            var host = _configuration[$"DatabaseHosts:{engineName}"];

            if (string.IsNullOrEmpty(host))
            {
                _logger.LogWarning($"⚠️ Host not configured for {engineName}, using localhost");
                host = "localhost";
            }

            _logger.LogInformation($"🌐 Building connection string with host: {host}");

            return engine switch
            {
                DatabaseEngine.PostgreSQL =>
                    $"Host={host};Port={port};Database={dbName};Username={credentials.Username};Password={credentials.Password}",

                DatabaseEngine.MySQL =>
                    $"Server={host};Port={port};Database={dbName};Uid={credentials.Username};Pwd={credentials.Password}",

                DatabaseEngine.MongoDB =>
                    $"mongodb://{credentials.Username}:{credentials.Password}@{host}:{port}/{dbName}?authSource={dbName}",

                _ => throw new NotSupportedException()
            };
        }
    }
}