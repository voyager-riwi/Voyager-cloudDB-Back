using CrudCloudDb.Application.Interfaces.Repositories;
using CrudCloudDb.Core.Entities;
using CrudCloudDb.Core.Enums; // Asegúrate de tener este 'using' de develop
using CrudCloudDb.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CrudCloudDb.Infrastructure.Repositories;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly ApplicationDbContext _context;

    public SubscriptionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // Versión correcta de 'develop' (con SaveChangesAsync)
    public async Task<Subscription> CreateAsync(Subscription subscription)
    {
        await _context.Subscriptions.AddAsync(subscription);
        await _context.SaveChangesAsync();
        return subscription;
    }

    // Método nuevo de la rama de la compañera
    public async Task<Subscription?> FindByOrderIdAsync(string orderId)
    {
        return await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.MercadoPagoOrderId == orderId);
    }

    // Métodos existentes de 'develop'
    public async Task<List<Subscription>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.UserId == userId)
            .ToListAsync();
    }

    public async Task UpdateAsync(Subscription subscription)
    {
        _context.Subscriptions.Update(subscription);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Subscription>> GetAllActiveAsync()
    {
        return await _context.Subscriptions
            .Include(s => s.Plan)
            .Include(s => s.User)
            .Where(s => s.Status == SubscriptionStatus.Active)
            .ToListAsync();
    }

    public async Task<Subscription?> GetByIdAsync(Guid id)
    {
        return await _context.Subscriptions
            .Include(s => s.Plan)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id);
    }
}