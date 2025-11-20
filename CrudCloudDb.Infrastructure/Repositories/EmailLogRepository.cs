using CrudCloudDb.Application.Interfaces.Repositories;
using CrudCloudDb.Core.Entities;
using CrudCloudDb.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CrudCloudDb.Infrastructure.Repositories
{
    /// <summary>
    /// Repositorio para gestión de logs de emails
    /// </summary>
    public class EmailLogRepository : IEmailLogRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmailLogRepository> _logger;

        public EmailLogRepository(
            ApplicationDbContext context,
            ILogger<EmailLogRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<EmailLog> CreateAsync(EmailLog emailLog)
        {
            try
            {
                _context.EmailLogs.Add(emailLog);
                await _context.SaveChangesAsync();
                return emailLog;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating email log");
                throw;
            }
        }

        public async Task<EmailLog?> GetByIdAsync(Guid id)
        {
            try
            {
                return await _context.EmailLogs
                    .FirstOrDefaultAsync(e => e.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting email log by ID: {id}");
                throw;
            }
        }

        public async Task<IEnumerable<EmailLog>> GetByEmailAsync(string email)
        {
            try
            {
                return await _context.EmailLogs
                    .Where(e => e.ToEmail == email)
                    .OrderByDescending(e => e.SentAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting email logs for: {email}");
                throw;
            }
        }

        public async Task<IEnumerable<EmailLog>> GetRecentAsync(int count = 100)
        {
            try
            {
                return await _context.EmailLogs
                    .OrderByDescending(e => e.SentAt)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent email logs");
                throw;
            }
        }
    }
}