using CrudCloudDb.Core.Enums;

namespace CrudCloudDb.Core.Entities
{
    /// <summary>
    /// Entidad para registrar todos los emails enviados
    /// </summary>
    public class EmailLog
    {
        public Guid Id { get; set; }
        
        /// <summary>
        /// Email del destinatario
        /// </summary>
        public string ToEmail { get; set; }
        
        /// <summary>
        /// Asunto del email
        /// </summary>
        public string Subject { get; set; }
        
        /// <summary>
        /// Cuerpo del email (HTML)
        /// </summary>
        public string Body { get; set; }
        
        /// <summary>
        /// Tipo de email enviado
        /// </summary>
        public EmailType EmailType { get; set; }
        
        /// <summary>
        /// Fecha y hora de envío
        /// </summary>
        public DateTime SentAt { get; set; }
        
        /// <summary>
        /// Indica si el email se envió exitosamente
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Mensaje de error (si falló)
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}