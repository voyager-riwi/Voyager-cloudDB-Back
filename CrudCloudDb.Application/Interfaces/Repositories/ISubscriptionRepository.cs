using CrudCloudDb.Core.Entities;

namespace CrudCloudDb.Application.Interfaces.Repositories;

public interface ISubscriptionRepository
{
    Task<Subscription> CreateAsync(Subscription subscription);
}