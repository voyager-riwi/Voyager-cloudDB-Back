using CrudCloudDb.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CrudCloudDb.Application.Interfaces.Repositories
{
    public interface IDatabaseInstanceRepository
    {
        Task<DatabaseInstance?> GetByIdAsync(Guid id);
        Task<IEnumerable<DatabaseInstance>> GetAllActiveAsync();
        Task<IEnumerable<DatabaseInstance>> GetByUserIdAsync(Guid userId);
        Task<DatabaseInstance> CreateAsync(DatabaseInstance instance);
        Task UpdateAsync(DatabaseInstance instance);
        Task DeleteAsync(Guid id);
    }
}