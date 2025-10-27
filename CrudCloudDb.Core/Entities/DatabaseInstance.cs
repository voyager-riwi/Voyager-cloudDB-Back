
using System.ComponentModel.DataAnnotations;
using CrudCloudDb.Core.Enums;

namespace CrudCloudDb.Core.Entities
{
    public class DatabaseInstance
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public DatabaseEngine Engine { get; set; }

        [MaxLength(255)]
        public string? ContainerId { get; set; }

        [Required]
        [MaxLength(100)]
        public string DatabaseName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public int Port { get; set; }

        public DatabaseStatus Status { get; set; }

        public string? ConnectionString { get; set; }

        public bool CredentialsViewed { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? DeletedAt { get; set; }

        // Relationships
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
    }
}