using CrudCloudDb.Application.Interfaces.Repositories;
using CrudCloudDb.Core.Entities;
using CrudCloudDb.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CrudCloudDb.Infrastructure.Repositories
{
    public class EmailLogRepository : IEmailLogRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmailLogRepository> _logger;

        public EmailLogRepository(ApplicationDbContext context, ILogger<EmailLogRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task AddAsync(EmailLog log)
        {
            try
            {
                await _context.EmailLogs.AddAsync(log);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error al guardar el log de email en la base de datos.");
            }
        }
    }
}