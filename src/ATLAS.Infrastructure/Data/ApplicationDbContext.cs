using Entities = ATLAS.Domain.Entities;
using ATLAS.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

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
    }
}
