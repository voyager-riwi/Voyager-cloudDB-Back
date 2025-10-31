using CrudCloudDb.Core.Entities;

namespace CrudCloudDb.Application.Interfaces.Repositories
{
    /// <summary>
    /// Interfaz para el repositorio de logs de emails
    /// </summary>
    public interface IEmailLogRepository
    {
        Task<EmailLog> CreateAsync(EmailLog emailLog);
        Task<EmailLog?> GetByIdAsync(Guid id);
        Task<IEnumerable<EmailLog>> GetByEmailAsync(string email);
        Task<IEnumerable<EmailLog>> GetRecentAsync(int count = 100);
        
       
    }
}