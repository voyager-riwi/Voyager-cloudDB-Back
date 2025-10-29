using CrudCloudDb.Core.Entities;

namespace CrudCloudDb.Application.Interfaces.Repositories;

public class IUserRepository
{
    public async Task<User> GetByIdAsync(Guid userId)
    {
        throw new NotImplementedException();
    }
}