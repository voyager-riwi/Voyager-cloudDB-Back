using CrudCloudDb.Core.Entities;

namespace CrudCloudDb.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid userId);
    Task<User?> GetByEmailAsync(string email);
    Task<User> CreateAsync(User user);
    Task<bool> IsEmailTakenAsync(string email);
    
    Task<User?> GetByPasswordResetTokenAsync(string token);
    Task UpdateAsync(User user); 
    Task<User?> GetByIdWithPlanAsync(Guid id);
}