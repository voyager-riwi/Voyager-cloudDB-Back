using CrudCloudDb.Application.Interfaces.Repositories;
using CrudCloudDb.Core.Entities;
using CrudCloudDb.Infrastructure.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CrudCloudDb.Infrastructure.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;

        public AuditLogRepository(ApplicationDbContext context, ILogger<AuditLogRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task AddAsync(AuditLog log)
        {
            try
            {
                await _context.AuditLogs.AddAsync(log);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error al guardar el log de auditoría en la base de datos.");
                // Dependiendo de la política de la aplicación, podrías decidir si relanzar la excepción
                // o simplemente registrarla y continuar. Por ahora, solo la registramos.
            }
        }
    }
}