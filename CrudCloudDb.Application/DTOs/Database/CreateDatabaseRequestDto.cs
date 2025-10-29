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
        /// Nombre de la base de datos
        /// </summary>
        [Required(ErrorMessage = "Database name is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Database name must be between 3 and 50 characters")]
        [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Database name can only contain letters, numbers, hyphens and underscores")]
        public string DatabaseName { get; set; }
    }
}