// CrudCloudDb.Infrastructure/Services/PortManagerService.cs
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using CrudCloudDb.Core.Enums;
using CrudCloudDb.Application.Interfaces.Repositories;
using CrudCloudDb.Application.Services.Interfaces;

namespace CrudCloudDb.Infrastructure.Services
{
    public class PortManagerService : IPortManagerService
    {
        private readonly IDatabaseInstanceRepository _databaseRepository;
        private readonly ILogger<PortManagerService> _logger;
        private const int BASE_PORT_START = 10000;
        private const int BASE_PORT_END = 20000;
        private static readonly SemaphoreSlim _semaphore = new(1, 1);

        public PortManagerService(
            IDatabaseInstanceRepository databaseRepository,
            ILogger<PortManagerService> logger)
        {
            _databaseRepository = databaseRepository;
            _logger = logger;
        }

        public async Task<int> GetAvailablePortAsync(DatabaseEngine engine)
        {
            await _semaphore.WaitAsync();
            try
            {
                _logger.LogInformation($"🔍 [{engine}] Buscando puerto disponible...");

                var usedPorts = await GetUsedPortsAsync();
                var usedPortsSet = new HashSet<int>(usedPorts);

                _logger.LogInformation($"   Puertos en uso: {string.Join(", ", usedPorts.Take(10))}...");

                for (int port = BASE_PORT_START; port <= BASE_PORT_END; port++)
                {
                    if (usedPortsSet.Contains(port))
                        continue;

                    if (await IsPortAvailableAsync(port))
                    {
                        _logger.LogInformation($"✅ [{engine}] Puerto asignado: {port}");
                        return port;
                    }
                }

                throw new InvalidOperationException(
                    $"No hay puertos disponibles en el rango {BASE_PORT_START}-{BASE_PORT_END}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> IsPortAvailableAsync(int port)
        {
            try
            {
                var listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                listener.Stop();
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }

        public async Task ReleasePortAsync(int port)
        {
            _logger.LogInformation($"🔓 Puerto liberado: {port}");
            await Task.CompletedTask;
        }

        public async Task<IEnumerable<int>> GetUsedPortsAsync()
        {
            var databases = await _databaseRepository.GetAllActiveAsync();
            return databases.Select(db => db.Port).ToList();
        }
    }
}