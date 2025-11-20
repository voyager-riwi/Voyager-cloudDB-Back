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
        
    }
}