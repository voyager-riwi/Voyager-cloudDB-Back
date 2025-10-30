using CrudCloudDb.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace CrudCloudDb.Application.DTOs.Database
{
    /// <summary>
    /// DTO para solicitud de creación de base de datos
    /// </summary>
    public class CreateDatabaseRequestDto
    {
        /// <summary>
        /// Motor de base de datos (PostgreSQL, MySQL, MongoDB)
        /// </summary>
        [Required(ErrorMessage = "Database engine is required")]
        public DatabaseEngine Engine { get; set; }

        /// <summary>
        /// Nombre de la base de datos (solo letras, números y guión bajo)
        /// </summary>
        [Required(ErrorMessage = "Database name is required")]
        [StringLength(50, MinimumLength = 3, 
            ErrorMessage = "Database name must be between 3 and 50 characters")]
        [RegularExpression(@"^[a-zA-Z][a-zA-Z0-9_]*$", 
            ErrorMessage = "Database name must start with a letter and contain only letters, numbers, and underscores. No hyphens (-), spaces, or special characters allowed.")]
        public string DatabaseName { get; set; }
    }
}