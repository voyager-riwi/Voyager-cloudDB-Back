using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CrudCloudDb.Infrastructure.Data;
using CrudCloudDb.Application.Services.Interfaces;
using CrudCloudDb.Core.Enums;

namespace CrudCloudDb.Infrastructure.Jobs
{
    /// <summary>
    /// Background job que elimina permanentemente las bases de datos marcadas como eliminadas después de 30 días
    /// </summary>
    public class DatabaseCleanupJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DatabaseCleanupJob> _logger;

        public DatabaseCleanupJob(
            IServiceProvider serviceProvider,
            ILogger<DatabaseCleanupJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🧹 Database Cleanup Job started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupDeletedDatabasesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error in database cleanup job");
                }

                // Ejecutar una vez al día a las 3:00 AM UTC
                var now = DateTime.UtcNow;
                var next3AM = now.Date.AddDays(1).AddHours(3);
                var delay = next3AM - now;

                _logger.LogInformation($"⏰ Next cleanup run at {next3AM:yyyy-MM-dd HH:mm} UTC (in {delay.TotalHours:F1} hours)");
                await Task.Delay(delay, stoppingToken);
            }

            _logger.LogInformation("🛑 Database Cleanup Job stopped");
        }

        private async Task CleanupDeletedDatabasesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var dockerService = scope.ServiceProvider.GetRequiredService<IDockerService>();

            var cutoffDate = DateTime.UtcNow.AddDays(-30);

            _logger.LogInformation($"🔍 Looking for databases deleted before {cutoffDate:yyyy-MM-dd HH:mm} UTC (more than 30 days ago)");

            var toDelete = dbContext.DatabaseInstances
                .Where(db => db.Status == DatabaseStatus.Deleted
                          && db.DeletedAt.HasValue
                          && db.DeletedAt.Value < cutoffDate)
                .ToList();

            if (toDelete.Count == 0)
            {
                _logger.LogInformation("✅ No databases to permanently delete");
                return;
            }

            _logger.LogInformation($"🗑️ Found {toDelete.Count} database(s) to permanently delete");

            foreach (var db in toDelete)
            {
                try
                {
                    var daysDeleted = (DateTime.UtcNow - db.DeletedAt.Value).TotalDays;
                    _logger.LogInformation($"🗑️ Permanently deleting database: {db.DatabaseName} (engine: {db.Engine}, deleted {daysDeleted:F0} days ago)");

                    // Eliminar físicamente de los contenedores maestros
                    await dockerService.PermanentlyDeleteDatabaseAsync(db.Engine, db.DatabaseName, db.Username);

                    // Eliminar registro de la BD del sistema
                    dbContext.DatabaseInstances.Remove(db);

                    _logger.LogInformation($"✅ Database {db.DatabaseName} permanently deleted");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ Error permanently deleting database {db.DatabaseName}");
                    // Continuar con las demás bases de datos
                }
            }

            // Guardar cambios en la base de datos
            await dbContext.SaveChangesAsync();

            _logger.LogInformation($"✅ Cleanup completed: {toDelete.Count} database(s) permanently deleted");
        }
    }
}
