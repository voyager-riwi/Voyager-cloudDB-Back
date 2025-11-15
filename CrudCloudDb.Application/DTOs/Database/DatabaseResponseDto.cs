namespace CrudCloudDb.Application.DTOs.Database
{
    /// <summary>
    /// DTO para respuesta con información de base de datos
    /// </summary>
    public class DatabaseResponseDto
    {
        /// <summary>
        /// ID único de la base de datos
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Nombre de la base de datos
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Motor de base de datos (PostgreSQL, MySQL, MongoDB)
        /// </summary>
        public string Engine { get; set; }

        /// <summary>
        /// Estado actual (Running, Stopped, etc.)
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Puerto asignado
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Host/dominio de conexión
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Usuario de la base de datos
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Connection string completo
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Fecha de creación
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Fecha de eliminación (soft delete - período de gracia de 30 días)
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// Indica si las credenciales ya fueron vistas
        /// </summary>
        public bool CredentialsViewed { get; set; }

        /// <summary>
        /// ID del contenedor Docker (primeros 12 caracteres)
        /// </summary>
        public string ContainerId { get; set; }

        /// <summary>
        /// Indica si el contenedor está corriendo actualmente
        /// </summary>
        public bool IsRunning { get; set; }
    }
}