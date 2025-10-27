using CrudCloudDb.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace CrudCloudDb.Core.Entities
{
    public class WebhookConfig
    {
        public Guid Id { get; set; }
        
        [Required]
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        
        [Required]
        [Url]
        public string PayloadUrl { get; set; } = string.Empty;
        
        public string? Secret { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        // Example: Storing events as a comma-separated string or as a related entity
        public string SubscribedEvents { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}