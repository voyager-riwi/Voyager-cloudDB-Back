using CrudCloudDb.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace CrudCloudDb.Core.Entities
{
    public class Subscription
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        public Guid PlanId { get; set; }
        public Plan Plan { get; set; } = null!;
        
        public SubscriptionStatus Status { get; set; }
        
        public DateTime StartDate { get; set; }
        
        public DateTime EndDate { get; set; }
        
        public string? MercadoPagoSubscriptionId { get; set; } // O ID de la orden de pago
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}