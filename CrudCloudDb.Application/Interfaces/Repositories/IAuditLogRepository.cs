using CrudCloudDb.Core.Entities;

namespace CrudCloudDb.Application.Interfaces.Repositories
{
    public interface IAuditLogRepository
    {
        Task AddAsync(AuditLog log);
    }
}