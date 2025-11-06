using CrudCloudDb.Application.Interfaces.Repositories;
using CrudCloudDb.Core.Entities;
using CrudCloudDb.Infrastructure.Data;

namespace CrudCloudDb.Infrastructure.Repositories;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly ApplicationDbContext _context;

    public SubscriptionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Subscription> CreateAsync(Subscription subscription)
    {
        await _context.Subscriptions.AddAsync(subscription);
        return subscription;
    }
}