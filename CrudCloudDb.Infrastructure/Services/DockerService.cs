﻿using Docker.DotNet;
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
using Microsoft.Data.SqlClient;
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

        public async Task PermanentlyDeleteDatabaseAsync(DatabaseEngine engine, string dbName, string username)
        {
            try
            {
                _logger.LogInformation($"🗑️ Permanently deleting {engine} database: {dbName} (user: {username})");

                var masterContainer = await _masterContainerService.GetOrCreateMasterContainerAsync(engine);

                await DeleteDatabaseInsideMasterAsync(masterContainer, dbName, username, engine);

                _logger.LogInformation($"✅ {engine} database {dbName} permanently deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error permanently deleting database {dbName}");
                throw;
            }
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

        public async Task<MasterContainerInfo?> GetMasterContainerInfoAsync(DatabaseEngine engine)
        {
            return await _masterContainerService.GetMasterContainerInfoAsync(engine);
        }

        public async Task ResetPasswordInMasterAsync(
            MasterContainerInfo masterContainer,
            string username,
            string newPassword,
            DatabaseEngine engine)
        {
            await ResetPasswordInsideMasterAsync(masterContainer, username, newPassword, engine);
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

                case DatabaseEngine.SQLServer:
                    await CreateSQLServerDatabaseAsync(masterContainer, databaseName, credentials);
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

            // ⭐ OPTIMIZACIÓN: Solo revocar permisos en bases de datos del sistema
            // No necesitamos iterar sobre TODAS las bases de datos de prueba
            var systemDatabases = new List<string> { "postgres", "template1" }; // template0 no permite conexión

            // --- PASO 1: REVOCAR PERMISOS GLOBALES DEL ROL PUBLIC (UNA SOLA VEZ) ---
            await using (var conn = new NpgsqlConnection(connString))
            {
                await conn.OpenAsync();

                // 🔑 REVOCAR SELECT en pg_database para PUBLIC (una sola vez)
                try
                {
                    await using var cmd = new NpgsqlCommand("REVOKE SELECT ON pg_database FROM PUBLIC", conn);
                    await cmd.ExecuteNonQueryAsync();
                    _logger.LogInformation("🔒 Revoked SELECT on pg_database from PUBLIC");
                }
                catch { } // Ya revocado anteriormente

                // Crear el usuario con restricciones máximas
                await using (var cmd = new NpgsqlCommand($"CREATE USER {credentials.Username} WITH PASSWORD '{credentials.Password}' NOCREATEDB NOCREATEROLE NOSUPERUSER NOREPLICATION NOBYPASSRLS", conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                // 🔑 Revocar SELECT en pg_database para el usuario específico
                await using (var cmd = new NpgsqlCommand($"REVOKE SELECT ON pg_database FROM {credentials.Username}", conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                    _logger.LogInformation($"🔒 Revoked SELECT on pg_database from {credentials.Username}");
                }

                // Crear la base de datos usando template0 con retry logic
                await CreateDatabaseWithRetryAsync(conn, dbName, master.AdminUsername);

                _logger.LogInformation($"✅ Created user {credentials.Username} and database {dbName}");
            }

            // --- PASO 2: REVOCAR ACCESO SOLO EN BASES DE DATOS DEL SISTEMA ---
            // Esto es mucho más rápido que iterar sobre todas las DBs de prueba
            foreach (var systemDb in systemDatabases)
            {
                try
                {
                    var tempConnString = $"Host={master.Host};Port={master.Port};Database={systemDb};Username={master.AdminUsername};Password={master.AdminPassword}";
                    await using var tempConn = new NpgsqlConnection(tempConnString);
                    await tempConn.OpenAsync();

                    // Revocar permisos en esta base de datos del sistema
                    await using (var cmd = new NpgsqlCommand($"REVOKE ALL ON DATABASE {systemDb} FROM {credentials.Username}", tempConn))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }

                    await using (var cmd = new NpgsqlCommand($"REVOKE ALL ON SCHEMA public FROM {credentials.Username}", tempConn))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }

                    _logger.LogInformation($"🔒 Revoked access to system database: {systemDb}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"⚠️ Could not revoke access to {systemDb}: {ex.Message}");
                }
            }

            // --- PASO 3: OTORGAR PERMISOS SOLO EN SU BASE DE DATOS ---
            var newDbConnString = $"Host={master.Host};Port={master.Port};Database={dbName};Username={master.AdminUsername};Password={master.AdminPassword}";
            await using (var newDbConn = new NpgsqlConnection(newDbConnString))
            {
                await newDbConn.OpenAsync();

                // Otorgar CONNECT solo en esta DB
                await using (var cmd = new NpgsqlCommand($"GRANT CONNECT ON DATABASE {dbName} TO {credentials.Username}", newDbConn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                // Dar permisos completos en el schema public
                await using (var cmd = new NpgsqlCommand($"GRANT ALL ON SCHEMA public TO {credentials.Username}", newDbConn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                // Privilegios por defecto para objetos futuros
                await using (var cmd = new NpgsqlCommand($"ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO {credentials.Username}", newDbConn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                await using (var cmd = new NpgsqlCommand($"ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO {credentials.Username}", newDbConn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                _logger.LogInformation($"✅ Granted permissions on database {dbName}");
            }

            _logger.LogInformation($"✅ PostgreSQL database {dbName} created with ISOLATION (optimized)");
        }

        /// <summary>
        /// Crea una base de datos PostgreSQL con reintentos en caso de bloqueos de template1
        /// </summary>
        private async Task CreateDatabaseWithRetryAsync(NpgsqlConnection conn, string dbName, string owner)
        {
            const int maxRetries = 5;
            const int initialDelayMs = 100;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    // Usar template0 en lugar de template1 para evitar bloqueos
                    // template0 es inmutable y no tiene locks de concurrencia
                    await using var cmd = new NpgsqlCommand(
                        $"CREATE DATABASE {dbName} WITH OWNER = {owner} TEMPLATE = template0 ENCODING = 'UTF8'", 
                        conn);
                    await cmd.ExecuteNonQueryAsync();
                    
                    if (attempt > 1)
                    {
                        _logger.LogInformation($"✅ Database {dbName} created successfully on attempt {attempt}");
                    }
                    return; // Éxito
                }
                catch (PostgresException ex) when (ex.SqlState == "55006" && attempt < maxRetries)
                {
                    // 55006: source database is being accessed by other users
                    var delayMs = initialDelayMs * (int)Math.Pow(2, attempt - 1); // Exponential backoff
                    _logger.LogWarning($"⚠️ Database creation blocked (attempt {attempt}/{maxRetries}), retrying in {delayMs}ms...");
                    await Task.Delay(delayMs);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ Error creating database {dbName} on attempt {attempt}");
                    throw;
                }
            }

            throw new Exception($"Failed to create database {dbName} after {maxRetries} attempts due to template database locks");
        }

        private async Task CreateMySQLDatabaseAsync(
            MasterContainerInfo master,
            string dbName,
            CredentialsResult credentials)
        {
            var connString = $"Server={master.Host};Port={master.Port};User={master.AdminUsername};Password={master.AdminPassword};ConnectionTimeout=30;DefaultCommandTimeout=60";

            await using var conn = new MySqlConnection(connString);
            await conn.OpenAsync();

            // 🔒 PASO 1: Crear base de datos con retry
            await CreateMySQLDatabaseWithRetryAsync(conn, dbName);

            // 🔒 PASO 2: Crear usuario con retry
            await CreateMySQLUserWithRetryAsync(conn, credentials);

            // 🔒 PASO 3: Revocar acceso a bases de datos del sistema PRIMERO
            var systemDatabases = new[] { "mysql", "information_schema", "performance_schema", "sys" };
            foreach (var sysDb in systemDatabases)
            {
                try
                {
                    await using var revokeCmd = new MySqlCommand(
                        $"REVOKE ALL PRIVILEGES ON `{sysDb}`.* FROM '{credentials.Username}'@'%'", conn);
                    await revokeCmd.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"⚠️ Could not revoke access to {sysDb}: {ex.Message}");
                }
            }

            // 🔒 PASO 4: Revocar acceso a otras bases de datos de usuarios existentes
            await using (var cmd = new MySqlCommand(
                $"SELECT schema_name FROM information_schema.schemata WHERE schema_name NOT IN ('mysql', 'information_schema', 'performance_schema', 'sys', '{dbName}')", conn))
            {
                await using var reader = await cmd.ExecuteReaderAsync();
                var otherDatabases = new List<string>();
                while (await reader.ReadAsync())
                {
                    otherDatabases.Add(reader.GetString(0));
                }
                await reader.CloseAsync();

                foreach (var otherDb in otherDatabases)
                {
                    try
                    {
                        await using var revokeCmd = new MySqlCommand(
                            $"REVOKE ALL PRIVILEGES ON `{otherDb}`.* FROM '{credentials.Username}'@'%'", conn);
                        await revokeCmd.ExecuteNonQueryAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"⚠️ Could not revoke access to {otherDb}: {ex.Message}");
                    }
                }
            }

            // 🔒 PASO 5: OTORGAR permisos SOLO en SU base de datos (DESPUÉS de los REVOKEs)
            await using (var cmd = new MySqlCommand(
                $@"GRANT SELECT, INSERT, UPDATE, DELETE, CREATE, DROP, INDEX, ALTER, 
                   CREATE TEMPORARY TABLES, LOCK TABLES, EXECUTE, CREATE VIEW, SHOW VIEW, 
                   CREATE ROUTINE, ALTER ROUTINE, TRIGGER, REFERENCES 
                   ON `{dbName}`.* TO '{credentials.Username}'@'%'", conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // 🔒 PASO 6: Aplicar cambios
            await using (var cmd = new MySqlCommand("FLUSH PRIVILEGES", conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            _logger.LogInformation($"✅ MySQL database {dbName} created with ISOLATED access (user can only access their own database)");
        }

        /// <summary>
        /// Crea una base de datos MySQL con reintentos
        /// </summary>
        private async Task CreateMySQLDatabaseWithRetryAsync(MySqlConnection conn, string dbName)
        {
            const int maxRetries = 5;
            const int initialDelayMs = 100;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await using var cmd = new MySqlCommand($"CREATE DATABASE `{dbName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci", conn);
                    await cmd.ExecuteNonQueryAsync();
                    
                    if (attempt > 1)
                    {
                        _logger.LogInformation($"✅ MySQL database {dbName} created successfully on attempt {attempt}");
                    }
                    return;
                }
                catch (MySqlException ex) when ((ex.Number == 1205 || ex.Number == 1213) && attempt < maxRetries)
                {
                    // 1205: Lock wait timeout exceeded
                    // 1213: Deadlock found
                    var delayMs = initialDelayMs * (int)Math.Pow(2, attempt - 1);
                    _logger.LogWarning($"⚠️ MySQL database creation blocked (attempt {attempt}/{maxRetries}), retrying in {delayMs}ms...");
                    await Task.Delay(delayMs);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ Error creating MySQL database {dbName} on attempt {attempt}");
                    throw;
                }
            }

            throw new Exception($"Failed to create MySQL database {dbName} after {maxRetries} attempts");
        }

        /// <summary>
        /// Crea un usuario MySQL con reintentos
        /// </summary>
        private async Task CreateMySQLUserWithRetryAsync(MySqlConnection conn, CredentialsResult credentials)
        {
            const int maxRetries = 5;
            const int initialDelayMs = 100;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await using var cmd = new MySqlCommand(
                        $"CREATE USER '{credentials.Username}'@'%' IDENTIFIED BY '{credentials.Password}'", conn);
                    await cmd.ExecuteNonQueryAsync();
                    
                    if (attempt > 1)
                    {
                        _logger.LogInformation($"✅ MySQL user {credentials.Username} created successfully on attempt {attempt}");
                    }
                    return;
                }
                catch (MySqlException ex) when ((ex.Number == 1205 || ex.Number == 1213) && attempt < maxRetries)
                {
                    var delayMs = initialDelayMs * (int)Math.Pow(2, attempt - 1);
                    _logger.LogWarning($"⚠️ MySQL user creation blocked (attempt {attempt}/{maxRetries}), retrying in {delayMs}ms...");
                    await Task.Delay(delayMs);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ Error creating MySQL user {credentials.Username} on attempt {attempt}");
                    throw;
                }
            }

            throw new Exception($"Failed to create MySQL user {credentials.Username} after {maxRetries} attempts");
        }

        private async Task CreateMongoDBDatabaseAsync(
            MasterContainerInfo master,
            string dbName,
            CredentialsResult credentials)
        {
            var connString = $"mongodb://{master.AdminUsername}:{master.AdminPassword}@{master.Host}:{master.Port}/admin";

            var client = new MongoClient(connString);
            var adminDb = client.GetDatabase("admin");

            // 🔒 CREAR USUARIO CON PERMISOS LIMITADOS SOLO A SU BASE DE DATOS
            // El usuario SOLO podrá ver y acceder a su propia base de datos
            // NO podrá listar ni ver otras bases de datos
            var command = new MongoDB.Bson.BsonDocument
            {
                { "createUser", credentials.Username },
                { "pwd", credentials.Password },
                { "roles", new MongoDB.Bson.BsonArray
                    {
                        // 🔒 readWrite: Lectura y escritura SOLO en su DB
                        new MongoDB.Bson.BsonDocument
                        {
                            { "role", "readWrite" },
                            { "db", dbName }
                        },
                        // 🔒 dbAdmin: Administrar SOLO su DB (crear índices, etc.)
                        new MongoDB.Bson.BsonDocument
                        {
                            { "role", "dbAdmin" },
                            { "db", dbName }
                        }
                        // ❌ NO incluimos roles como:
                        // - "readAnyDatabase" (vería todas las DBs)
                        // - "dbAdminAnyDatabase" (administraría todas las DBs)
                        // - "userAdmin" (gestionaría usuarios)
                        // - "clusterMonitor" (vería info del cluster)
                    }
                }
            };

            await adminDb.RunCommandAsync<MongoDB.Bson.BsonDocument>(command);

            _logger.LogInformation($"✅ MongoDB database {dbName} created with MAXIMUM ISOLATION (user can ONLY see and access their own database)");
        }

        private async Task CreateSQLServerDatabaseAsync(
            MasterContainerInfo master,
            string dbName,
            CredentialsResult credentials)
        {
            var connString = $"Server={master.Host},{master.Port};Database=master;User Id={master.AdminUsername};Password={master.AdminPassword};TrustServerCertificate=True;Encrypt=False;Connection Timeout=30";

            await using var conn = new SqlConnection(connString);
            await conn.OpenAsync();

            // 🔒 PASO 1: Crear base de datos con retry
            await CreateSQLServerDatabaseWithRetryAsync(conn, dbName);

            // 🔒 PASO 2: Crear login a nivel servidor con retry
            await CreateSQLServerLoginWithRetryAsync(conn, credentials);

            // 🔒 PASO 3: Cambiar a la base de datos recién creada para crear usuario
            var dbConnString = $"Server={master.Host},{master.Port};Database={dbName};User Id={master.AdminUsername};Password={master.AdminPassword};TrustServerCertificate=True;Encrypt=False;Connection Timeout=30";
            await using var dbConn = new SqlConnection(dbConnString);
            await dbConn.OpenAsync();

            // 🔒 PASO 4: Crear usuario en la base de datos y asignar roles
            await using (var cmd = new SqlCommand(
                $"CREATE USER [{credentials.Username}] FOR LOGIN [{credentials.Username}]", dbConn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // 🔒 PASO 5: Asignar roles mínimos necesarios
            // db_datareader: Leer datos
            // db_datawriter: Escribir datos
            // db_ddladmin: Crear/modificar esquema (tablas, índices, etc.)
            var roles = new[] { "db_datareader", "db_datawriter", "db_ddladmin" };
            foreach (var role in roles)
            {
                await using var cmd = new SqlCommand(
                    $"ALTER ROLE {role} ADD MEMBER [{credentials.Username}]", dbConn);
                await cmd.ExecuteNonQueryAsync();
            }

            // 🔒 PASO 6: Denegar acceso a bases de datos del sistema
            await using (var masterConn = new SqlConnection(connString))
            {
                await masterConn.OpenAsync();

                var systemDatabases = new[] { "master", "model", "msdb", "tempdb" };
                foreach (var sysDb in systemDatabases)
                {
                    try
                    {
                        await using var cmd = new SqlCommand(
                            $"DENY CONNECT TO [{credentials.Username}]", masterConn);
                        cmd.CommandText = $"USE [{sysDb}]; DENY CONNECT TO [{credentials.Username}]";
                        await cmd.ExecuteNonQueryAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"⚠️ Could not deny access to {sysDb}: {ex.Message}");
                    }
                }

                // 🔒 PASO 7: Denegar permisos VIEW DEFINITION a nivel servidor
                try
                {
                    await using var cmd = new SqlCommand(
                        $"DENY VIEW ANY DATABASE TO [{credentials.Username}]", masterConn);
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"⚠️ Could not deny VIEW ANY DATABASE: {ex.Message}");
                }
            }

            _logger.LogInformation($"✅ SQL Server database {dbName} created with ISOLATED access (user can only access their own database)");
        }

        /// <summary>
        /// Crea una base de datos SQL Server con reintentos
        /// </summary>
        private async Task CreateSQLServerDatabaseWithRetryAsync(SqlConnection conn, string dbName)
        {
            const int maxRetries = 5;
            const int initialDelayMs = 100;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await using var cmd = new SqlCommand($"CREATE DATABASE [{dbName}]", conn);
                    await cmd.ExecuteNonQueryAsync();
                    
                    if (attempt > 1)
                    {
                        _logger.LogInformation($"✅ SQL Server database {dbName} created successfully on attempt {attempt}");
                    }
                    return;
                }
                catch (SqlException ex) when ((ex.Number == 1205 || ex.Number == 1222) && attempt < maxRetries)
                {
                    // 1205: Lock wait timeout exceeded
                    // 1222: Lock request timeout period exceeded
                    var delayMs = initialDelayMs * (int)Math.Pow(2, attempt - 1);
                    _logger.LogWarning($"⚠️ SQL Server database creation blocked (attempt {attempt}/{maxRetries}), retrying in {delayMs}ms...");
                    await Task.Delay(delayMs);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ Error creating SQL Server database {dbName} on attempt {attempt}");
                    throw;
                }
            }

            throw new Exception($"Failed to create SQL Server database {dbName} after {maxRetries} attempts");
        }

        /// <summary>
        /// Crea un login SQL Server con reintentos
        /// </summary>
        private async Task CreateSQLServerLoginWithRetryAsync(SqlConnection conn, CredentialsResult credentials)
        {
            const int maxRetries = 5;
            const int initialDelayMs = 100;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await using var cmd = new SqlCommand(
                        $"CREATE LOGIN [{credentials.Username}] WITH PASSWORD = '{credentials.Password}', CHECK_POLICY = OFF", conn);
                    await cmd.ExecuteNonQueryAsync();
                    
                    if (attempt > 1)
                    {
                        _logger.LogInformation($"✅ SQL Server login {credentials.Username} created successfully on attempt {attempt}");
                    }
                    return;
                }
                catch (SqlException ex) when ((ex.Number == 1205 || ex.Number == 1222) && attempt < maxRetries)
                {
                    var delayMs = initialDelayMs * (int)Math.Pow(2, attempt - 1);
                    _logger.LogWarning($"⚠️ SQL Server login creation blocked (attempt {attempt}/{maxRetries}), retrying in {delayMs}ms...");
                    await Task.Delay(delayMs);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ Error creating SQL Server login {credentials.Username} on attempt {attempt}");
                    throw;
                }
            }

            throw new Exception($"Failed to create SQL Server login {credentials.Username} after {maxRetries} attempts");
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

                case DatabaseEngine.SQLServer:
                    await DeleteSQLServerDatabaseAsync(master, dbName, username);
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

        private async Task DeleteSQLServerDatabaseAsync(
            MasterContainerInfo master,
            string dbName,
            string username)
        {
            var connString = $"Server={master.Host},{master.Port};Database=master;User Id={master.AdminUsername};Password={master.AdminPassword};TrustServerCertificate=True;Encrypt=False;Connection Timeout=30";

            await using var conn = new SqlConnection(connString);
            await conn.OpenAsync();

            // 🔒 PASO 1: Terminar todas las conexiones activas a la base de datos
            await using (var cmd = new SqlCommand(
                $"ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE", conn))
            {
                try
                {
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"⚠️ Could not set database to SINGLE_USER: {ex.Message}");
                }
            }

            // 🔒 PASO 2: Eliminar la base de datos
            await using (var cmd = new SqlCommand($"DROP DATABASE IF EXISTS [{dbName}]", conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // 🔒 PASO 3: Eliminar el login
            await using (var cmd = new SqlCommand($"DROP LOGIN IF EXISTS [{username}]", conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            _logger.LogInformation($"✅ SQL Server database {dbName} deleted");
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

                case DatabaseEngine.SQLServer:
                    await ResetSQLServerPasswordAsync(master, username, newPassword);
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

        private async Task ResetSQLServerPasswordAsync(
            MasterContainerInfo master,
            string username,
            string newPassword)
        {
            var connString = $"Server={master.Host},{master.Port};Database=master;User Id={master.AdminUsername};Password={master.AdminPassword};TrustServerCertificate=True;Encrypt=False;Connection Timeout=30";

            await using var conn = new SqlConnection(connString);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(
                $"ALTER LOGIN [{username}] WITH PASSWORD = '{newPassword}'", conn);
            await cmd.ExecuteNonQueryAsync();

            _logger.LogInformation($"✅ SQL Server password reset for {username}");
        }

        private string BuildConnectionString(
            DatabaseEngine engine,
            int port,
            string dbName,
            CredentialsResult credentials)
        {
            var engineName = engine.ToString();
            
            // Leer desde variables de entorno primero, luego appsettings
            var envVarName = $"DB_HOST_{engineName.ToUpperInvariant()}";
            var host = Environment.GetEnvironmentVariable(envVarName)
                      ?? _configuration[$"DatabaseHosts:{engineName}"];

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

                DatabaseEngine.SQLServer =>
                    $"Server={host},{port};Database={dbName};User Id={credentials.Username};Password={credentials.Password};TrustServerCertificate=True;Encrypt=False",

                _ => throw new NotSupportedException()
            };
        }
    }
}