using System.ComponentModel.DataAnnotations;

namespace CrudCloudDb.Core.Entities
{
    /// <summary>
    /// Configuración de webhooks para notificaciones
    /// </summary>
    public class WebhookConfig
    {
        public Guid Id { get; set; }
        
        /// <summary>
        /// Usuario propietario del webhook
        /// </summary>
        public Guid UserId { get; set; }
        public User User { get; set; }
        
        /// <summary>
        /// URL del webhook
        /// </summary>
        [Required]
        [MaxLength(500)]
        [Url]
        public string Url { get; set; } = string.Empty;
        
        /// <summary>
        /// Secret para firmar las peticiones (opcional)
        /// </summary>
        [MaxLength(255)]
        public string? Secret { get; set; }
        
        /// <summary>
        /// Indica si el webhook está activo
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// Eventos suscritos (separados por coma: DatabaseCreated,DatabaseDeleted)
        /// </summary>
        [MaxLength(500)]
        public string SubscribedEvents { get; set; } = string.Empty;
        
        /// <summary>
        /// Fecha de creación
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Última actualización
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}