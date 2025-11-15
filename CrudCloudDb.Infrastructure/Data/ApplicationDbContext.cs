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

            // ============================================
            // User
            // ============================================
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                
                entity.HasOne(u => u.CurrentPlan)
                      .WithMany(p => p.Users)
                      .HasForeignKey(u => u.CurrentPlanId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
            
            // ============================================
            // Plan
            // ============================================
            modelBuilder.Entity<Plan>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.PlanType).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            });

            // ============================================
            // Subscription
            // ============================================
            modelBuilder.Entity<Subscription>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasOne(s => s.User)
                      .WithMany(u => u.Subscriptions)
                      .HasForeignKey(s => s.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(s => s.Plan)
                      .WithMany()
                      .HasForeignKey(s => s.PlanId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ============================================
            // DatabaseInstance
            // ============================================
            modelBuilder.Entity<DatabaseInstance>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasOne(d => d.User)
                      .WithMany(u => u.DatabaseInstances)
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.DatabaseName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.ConnectionString).IsRequired();
                entity.Property(e => e.MasterContainerId).IsRequired().HasMaxLength(100);
                
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CreatedAt);
            });

            // ============================================
            // WebhookConfig
            // ============================================
            modelBuilder.Entity<WebhookConfig>(entity =>
            {
                entity.HasKey(e => e.Id);
    
                entity.HasOne(w => w.User)
                    .WithMany()
                    .HasForeignKey(w => w.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
    
                entity.Property(e => e.Url)
                    .IsRequired()
                    .HasMaxLength(500);
    
                entity.Property(e => e.Secret)
                    .HasMaxLength(255);
    
                entity.Property(e => e.SubscribedEvents)
                    .IsRequired()
                    .HasMaxLength(500);
    
                entity.Property(e => e.IsActive)
                    .IsRequired();
    
                entity.Property(e => e.CreatedAt)
                    .IsRequired();
    
                // Índices
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.CreatedAt);
            });

            // ============================================
            // AuditLog ⭐ CORREGIDO
            // ============================================
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // ⭐ NO tiene navegación a User, solo UserId nullable
                entity.Property(e => e.UserId).IsRequired(false);
                
                entity.Property(e => e.Action)
                      .IsRequired()
                      .HasMaxLength(255);
                
                entity.Property(e => e.EntityName)
                      .HasMaxLength(100);
                
                entity.Property(e => e.EntityId);
                
                entity.Property(e => e.Changes); // JSON
                
                entity.Property(e => e.Timestamp)
                      .IsRequired();
                
                // Índices para búsquedas
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.Action);
                entity.HasIndex(e => e.EntityName);
            });

            // ============================================
            // EmailLog
            // ============================================
            modelBuilder.Entity<EmailLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.ToEmail).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Subject).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Body).IsRequired();
                entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
                
                entity.HasIndex(e => e.ToEmail);
                entity.HasIndex(e => e.SentAt);
                entity.HasIndex(e => e.EmailType);
                entity.HasIndex(e => e.Success);
            });

            // ============================================
            // Seed Data
            // ============================================
            PlanSeeder.Seed(modelBuilder);
        }
    }
}