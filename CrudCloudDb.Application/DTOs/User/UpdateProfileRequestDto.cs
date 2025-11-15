using System.ComponentModel.DataAnnotations;

namespace CrudCloudDb.Application.DTOs.User;

public class UpdateProfileRequestDto
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres")]
    public string FirstName { get; set; }
    
    [Required(ErrorMessage = "El apellido es obligatorio")]
    [MaxLength(100, ErrorMessage = "El apellido no puede superar los 100 caracteres")]
    public string LastName { get; set; }
}