using System.ComponentModel.DataAnnotations;

namespace CrudCloudDb.Core.Entities
{
    public class EmailLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        [EmailAddress]
        public string To { get; set; } = string.Empty;
        
        [Required]
        public string Subject { get; set; } = string.Empty;
        
        [Required]
        public string Body { get; set; } = string.Empty;
        
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        
        public bool IsSent { get; set; }
        
        public string? ErrorMessage { get; set; }
    }
}