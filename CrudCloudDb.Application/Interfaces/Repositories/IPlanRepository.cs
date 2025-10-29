using CrudCloudDb.Core.Entities;
using CrudCloudDb.Core.Enums;

namespace CrudCloudDb.Application.Interfaces.Repositories;

public interface IPlanRepository
{   
    Task<Plan?> GetDefaultPlanAsync();
    Task<Plan?> GetByPlanTypeAsync(PlanType planType);
}