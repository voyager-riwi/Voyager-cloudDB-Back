using CrudCloudDb.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrudCloudDb.Core.Entities
{
    public class Plan
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public PlanType PlanType { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        public int DatabaseLimitPerEngine { get; set; }
        
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}