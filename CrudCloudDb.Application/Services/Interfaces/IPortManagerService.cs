using CrudCloudDb.Core.Enums;

namespace CrudCloudDb.Application.Services.Interfaces
{
    public interface IPortManagerService
    {
        Task<int> GetAvailablePortAsync(DatabaseEngine engine);
        Task<bool> IsPortAvailableAsync(int port);
        Task ReleasePortAsync(int port);
        Task<IEnumerable<int>> GetUsedPortsAsync();
    }
}