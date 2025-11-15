using System.ComponentModel.DataAnnotations;

namespace CrudCloudDb.Application.DTOs.Payment;

public class CreatePreferenceRequestDto
{
    [Required(ErrorMessage = "El ID del plan es obligatorio.")]
    public Guid PlanId { get; set; }
}