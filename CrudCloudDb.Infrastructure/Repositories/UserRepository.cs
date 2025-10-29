using CrudCloudDb.Application.Interfaces.Repositories;
using CrudCloudDb.Core.Entities;
using CrudCloudDb.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CrudCloudDb.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User> CreateAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.CurrentPlan) // Incluimos el Plan para tener la info completa
            .FirstOrDefaultAsync(u => u.Email == email);
    }
    
    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<bool> IsEmailTakenAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }
}