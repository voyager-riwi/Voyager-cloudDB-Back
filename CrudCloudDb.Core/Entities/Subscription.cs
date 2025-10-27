using CrudCloudDb.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrudCloudDb.Core.Entities
{
    public class Subscription
    {
        public Guid Id { get; set; }
        
        [Required]
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        public Guid PlanId { get; set; }
        public Plan Plan { get; set; } = null!;
        
        public SubscriptionStatus Status { get; set; }
        
        public DateTime StartDate { get; set; }
        
        public DateTime EndDate { get; set; }
        
        public string? MercadoPagoSubscriptionId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}