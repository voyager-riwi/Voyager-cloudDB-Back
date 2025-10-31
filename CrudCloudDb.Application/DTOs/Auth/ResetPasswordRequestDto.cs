using System.ComponentModel.DataAnnotations;

namespace CrudCloudDb.Application.DTOs.Auth;

public class ResetPasswordRequestDto
{
    [Required]
    public string Token { get; set; }
    
    [Required(ErrorMessage = "La nueva contraseña es obligatoria") ]
    [MinLength(8, ErrorMessage = "La nueva contraseña debe tener al menos 8 caracteres.")]
    public string NewPassword  { get;set; }
    
    [Required(ErrorMessage = "La confirmación de la contraseña es obligatoria.")]
    [Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden.")]
    public string confirmNewPassword {get; set;}
    
}