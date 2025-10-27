using System.ComponentModel.DataAnnotations;

namespace CrudCloudDb.Core.Entities
{
    public class AuditLog
    {
        public long Id { get; set; }
        
        public Guid? UserId { get; set; } // Can be null for system events
        
        [Required]
        [MaxLength(255)]
        public string Action { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? EntityName { get; set; }
        
        public string? EntityId { get; set; }
        
        public string? Changes { get; set; } // JSON formatted string of changes
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}