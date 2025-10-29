using CrudCloudDb.Core.Entities;

namespace CrudCloudDb.Application.Interfaces.Repositories
{
    public interface IEmailLogRepository
    {
        Task AddAsync(EmailLog log);
    }
}