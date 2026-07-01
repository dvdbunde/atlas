using Entities = ATLAS.Domain.Entities;
using ATLAS.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ATLAS.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Entities.Application> Applications { get; set; }
        public DbSet<Entities.PermitType> PermitTypes { get; set; }
        public DbSet<Entities.User> Users { get; set; }
        public DbSet<Entities.AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.ApplyConfiguration(new ApplicationConfiguration());
            modelBuilder.ApplyConfiguration(new PermitTypeConfiguration());
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
        }

        public override int SaveChanges()
        {
            LogChangeTracker();            

            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            LogChangeTracker();            

            return await base.SaveChangesAsync(cancellationToken);
        }

        private void LogChangeTracker()
        {
            Debug.WriteLine("========== EF CHANGE TRACKER ==========");

            foreach (EntityEntry entry in ChangeTracker.Entries())
            {
                Debug.WriteLine(
                    $"{entry.Entity.GetType().Name,-20} State={entry.State}");
            }

            Debug.WriteLine("=======================================");
        }
    }
}
