using CrudCloudDb.Application.Services.Interfaces;
using CrudCloudDb.Application.Interfaces.Repositories;
using CrudCloudDb.Core.Entities;
using CrudCloudDb.Core.Enums;
using Microsoft.Extensions.Logging;

namespace CrudCloudDb.Application.Services.Implementation
{
    /// <summary>
    /// Servicio de gestión de suscripciones
    /// Maneja la activación/cancelación de planes Premium cuando Mercado Pago confirma pagos
    /// </summary>
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPlanRepository _planRepository;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(
            ISubscriptionRepository subscriptionRepository,
            IUserRepository userRepository,
            IPlanRepository planRepository,
            ILogger<SubscriptionService> logger)
        {
            _subscriptionRepository = subscriptionRepository;
            _userRepository = userRepository;
            _planRepository = planRepository;
            _logger = logger;
        }

        /// <summary>
        /// Activa una suscripción Premium cuando Mercado Pago confirma el pago
        /// </summary>
        public async Task<Subscription> ActivatePremiumSubscriptionAsync(
            Guid userId,
            PlanType planType,
            string mercadoPagoPaymentId,
            int durationMonths = 1)
        {
            _logger.LogInformation($"💳 Activating {planType} subscription for user {userId} (Mercado Pago Payment: {mercadoPagoPaymentId})");

            // 1. Obtener usuario
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogError($"❌ User {userId} not found");
                throw new KeyNotFoundException("User not found");
            }

            // 2. Obtener el plan Premium
            var premiumPlan = await _planRepository.GetByTypeAsync(planType);
            if (premiumPlan == null)
            {
                _logger.LogError($"❌ Plan {planType} not found in database");
                throw new KeyNotFoundException($"Plan {planType} not found");
            }

            // 3. Cancelar suscripción activa anterior (si existe)
            var existingSubscription = await GetActiveSubscriptionAsync(userId);
            if (existingSubscription != null)
            {
                _logger.LogInformation($"⚠️ Cancelling existing subscription {existingSubscription.Id}");
                existingSubscription.Status = SubscriptionStatus.Cancelled;
                await _subscriptionRepository.UpdateAsync(existingSubscription);
            }

            // 4. Crear nueva suscripción Premium
            var startDate = DateTime.UtcNow;
            var endDate = startDate.AddMonths(durationMonths);

            var newSubscription = new Subscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PlanId = premiumPlan.Id,
                Status = SubscriptionStatus.Active,
                StartDate = startDate,
                EndDate = endDate,
                MercadoPagoPaymentId = mercadoPagoPaymentId,
                CreatedAt = DateTime.UtcNow
            };

            await _subscriptionRepository.CreateAsync(newSubscription);

            // 5. ⭐ CAMBIAR EL PLAN ACTUAL DEL USUARIO ⭐
            user.CurrentPlanId = premiumPlan.Id;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation($"✅ Premium subscription activated for user {userId}");
            _logger.LogInformation($"📊 User can now create up to {premiumPlan.DatabaseLimitPerEngine} databases per engine (was 2 in Free plan)");
            _logger.LogInformation($"⏰ Subscription valid until {endDate:yyyy-MM-dd}");

            return newSubscription;
        }

        /// <summary>
        /// Cancela una suscripción y revierte al plan Free
        /// </summary>
        public async Task<bool> CancelSubscriptionAsync(Guid userId)
        {
            _logger.LogInformation($"🚫 Cancelling subscription for user {userId}");

            var activeSubscription = await GetActiveSubscriptionAsync(userId);
            if (activeSubscription == null)
            {
                _logger.LogWarning($"⚠️ No active subscription found for user {userId}");
                return false;
            }

            // 1. Marcar suscripción como cancelada
            activeSubscription.Status = SubscriptionStatus.Cancelled;
            await _subscriptionRepository.UpdateAsync(activeSubscription);

            // 2. ⭐ REVERTIR AL PLAN FREE ⭐
            var freePlan = await _planRepository.GetByTypeAsync(PlanType.Free);
            if (freePlan == null)
            {
                _logger.LogError("❌ Free plan not found in database");
                throw new KeyNotFoundException("Free plan not found");
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogError($"❌ User {userId} not found");
                return false;
            }

            user.CurrentPlanId = freePlan.Id;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation($"✅ Subscription cancelled. User reverted to Free plan (2 DBs per engine)");

            return true;
        }

        /// <summary>
        /// Obtiene la suscripción activa de un usuario
        /// </summary>
        public async Task<Subscription?> GetActiveSubscriptionAsync(Guid userId)
        {
            var subscriptions = await _subscriptionRepository.GetByUserIdAsync(userId);
            return subscriptions
                .Where(s => s.Status == SubscriptionStatus.Active)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefault();
        }

        /// <summary>
        /// Verifica si hay suscripciones expiradas y las cancela automáticamente
        /// Este método debe ser llamado por un Job programado (cada día)
        /// </summary>
        public async Task CheckAndCancelExpiredSubscriptionsAsync()
        {
            _logger.LogInformation("🔍 Checking for expired subscriptions...");

            var allActiveSubscriptions = await _subscriptionRepository.GetAllActiveAsync();
            var now = DateTime.UtcNow;
            var expiredCount = 0;

            foreach (var subscription in allActiveSubscriptions)
            {
                if (subscription.EndDate < now)
                {
                    _logger.LogInformation($"⏰ Subscription {subscription.Id} expired on {subscription.EndDate:yyyy-MM-dd}. Cancelling...");

                    // Cancelar suscripción y revertir al Free plan
                    await CancelSubscriptionAsync(subscription.UserId);
                    expiredCount++;
                }
            }

            if (expiredCount > 0)
            {
                _logger.LogInformation($"✅ Cancelled {expiredCount} expired subscription(s)");
            }
            else
            {
                _logger.LogInformation("✅ No expired subscriptions found");
            }
        }

        /// <summary>
        /// Obtiene el historial de suscripciones de un usuario
        /// </summary>
        public async Task<List<Subscription>> GetUserSubscriptionHistoryAsync(Guid userId)
        {
            _logger.LogInformation($"📜 Getting subscription history for user {userId}");
            var subscriptions = await _subscriptionRepository.GetByUserIdAsync(userId);
            return subscriptions.OrderByDescending(s => s.StartDate).ToList();
        }
    }
}

