using CrudCloudDb.Core.Entities;
using CrudCloudDb.Core.Enums;

namespace CrudCloudDb.Application.Services.Interfaces
{
    /// <summary>
    /// Servicio para gestión de suscripciones y cambios de plan
    /// Este servicio será usado por Mercado Pago cuando se complete un pago
    /// </summary>
    public interface ISubscriptionService
    {
        /// <summary>
        /// Activa una suscripción Premium cuando Mercado Pago confirma el pago
        /// </summary>
        /// <param name="userId">ID del usuario que pagó</param>
        /// <param name="planType">Tipo de plan (Premium)</param>
        /// <param name="mercadoPagoPaymentId">ID del pago de Mercado Pago</param>
        /// <param name="durationMonths">Duración de la suscripción en meses (1, 6, 12)</param>
        /// <returns>La suscripción activada</returns>
        Task<Subscription> ActivatePremiumSubscriptionAsync(
            Guid userId, 
            PlanType planType, 
            string mercadoPagoPaymentId,
            int durationMonths = 1);

        /// <summary>
        /// Cancela una suscripción y revierte al plan Free
        /// </summary>
        Task<bool> CancelSubscriptionAsync(Guid userId);

        /// <summary>
        /// Obtiene la suscripción activa de un usuario
        /// </summary>
        Task<Subscription?> GetActiveSubscriptionAsync(Guid userId);

        /// <summary>
        /// Verifica si una suscripción ha expirado y la cancela automáticamente
        /// Este método será llamado por un Job programado
        /// </summary>
        Task CheckAndCancelExpiredSubscriptionsAsync();

        /// <summary>
        /// Obtiene el historial de suscripciones de un usuario
        /// </summary>
        Task<List<Subscription>> GetUserSubscriptionHistoryAsync(Guid userId);
    }
}

