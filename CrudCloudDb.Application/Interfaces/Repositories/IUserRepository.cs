using CrudCloudDb.Core.Entities;

namespace CrudCloudDb.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid userId);
    Task<User?> GetByEmailAsync(string email);
    Task<User> CreateAsync(User user);
    Task<bool> IsEmailTakenAsync(string email);
}