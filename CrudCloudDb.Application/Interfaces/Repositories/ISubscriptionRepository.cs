﻿using CrudCloudDb.Core.Entities;

namespace CrudCloudDb.Application.Interfaces.Repositories;

public interface ISubscriptionRepository
{
    Task<Subscription> CreateAsync(Subscription subscription);
    
    /// <summary>
    /// Obtiene todas las suscripciones de un usuario
    /// </summary>
    Task<List<Subscription>> GetByUserIdAsync(Guid userId);
    
    /// <summary>
    /// Actualiza una suscripción (para cambiar status, etc.)
    /// </summary>
    Task UpdateAsync(Subscription subscription);
    
    /// <summary>
    /// Obtiene todas las suscripciones activas (para el job de expiración)
    /// </summary>
    Task<List<Subscription>> GetAllActiveAsync();
    
    /// <summary>
    /// Obtiene una suscripción por ID
    /// </summary>
    Task<Subscription?> GetByIdAsync(Guid id);
}