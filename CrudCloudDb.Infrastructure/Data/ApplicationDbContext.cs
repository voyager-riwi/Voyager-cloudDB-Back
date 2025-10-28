using Microsoft.EntityFrameworkCore;
using CrudCloudDb.Core.Entities;
using CrudCloudDb.Infrastructure.Data.Seeders;

namespace CrudCloudDb.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        
        public DbSet<User> Users { get; set; }
        public DbSet<Plan> Plans { get; set; }
        public DbSet<DatabaseInstance> DatabaseInstances { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<WebhookConfig> WebhookConfigs { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<EmailLog> EmailLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique(); 
                
                entity.HasOne(u => u.CurrentPlan)
                      .WithMany(p => p.Users)
                      .HasForeignKey(u => u.CurrentPlanId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
            
            
            modelBuilder.Entity<Plan>(entity =>
            {
                entity.HasIndex(e => e.PlanType).IsUnique();
            });

     
            modelBuilder.Entity<Subscription>(entity =>
            {

                entity.HasOne(s => s.Plan)
                      .WithMany()
                      .HasForeignKey(s => s.PlanId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
            
            PlanSeeder.Seed(modelBuilder);
        }
    }
}