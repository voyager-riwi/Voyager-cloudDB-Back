﻿using CrudCloudDb.Core.Entities;
using CrudCloudDb.Core.Enums;

namespace CrudCloudDb.Application.Interfaces.Repositories;

public interface IPlanRepository
{   
    Task<Plan?> GetDefaultPlanAsync();
    Task<Plan?> GetByPlanTypeAsync(PlanType planType);
    Task<Plan?> GetByIdAsync(Guid id);
    Task<IEnumerable<Plan>> GetAllAsync();
    
    /// <summary>
    /// Obtiene un plan por su tipo (Free, Premium, etc.)
    /// Usado por SubscriptionService al activar/cancelar suscripciones
    /// </summary>
    Task<Plan?> GetByTypeAsync(PlanType planType);
}