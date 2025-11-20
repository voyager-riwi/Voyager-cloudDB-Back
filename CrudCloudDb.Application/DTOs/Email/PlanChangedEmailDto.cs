namespace CrudCloudDb.Application.DTOs.Email
{
    /// <summary>
    /// DTO para email al cambiar o renovar plan
    /// </summary>
    public class PlanChangedEmailDto
    {
        public string UserEmail { get; set; }
        public string UserName { get; set; }
        public string OldPlanName { get; set; }
        public string NewPlanName { get; set; }
        public decimal NewPlanPrice { get; set; }
        public DateTime ChangedAt { get; set; }
        public DateTime? NextBillingDate { get; set; }
        public bool IsRenewal { get; set; } // true = renovación, false = cambio de plan
    }
}