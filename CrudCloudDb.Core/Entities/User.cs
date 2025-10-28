using CrudCloudDb.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace CrudCloudDb.Core.Entities
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        public bool EmailVerified { get; set; } = false;
        public string? VerificationToken { get; set; }
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetExpires { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }

        // Relationships
        public Guid CurrentPlanId { get; set; }
        public Plan CurrentPlan { get; set; } = null!;

        public ICollection<DatabaseInstance> Databases { get; set; } = new List<DatabaseInstance>();
        public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
        public ICollection<WebhookConfig> WebhookConfigs { get; set; } = new List<WebhookConfig>();
    }
}
