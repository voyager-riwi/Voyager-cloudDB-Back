using CrudCloudDb.Core.Enums;

namespace CrudCloudDb.Application.DTOs.Plan
{
    public class PlanDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public PlanType PlanType { get; set; }
        public decimal Price { get; set; }
        public int DatabaseLimitPerEngine { get; set; }
    }
}