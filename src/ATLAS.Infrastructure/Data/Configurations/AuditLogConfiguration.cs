using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ATLAS.Domain.Entities;

namespace ATLAS.Infrastructure.Data.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Id).HasDefaultValueSql("NEWID()");
            builder.Property(a => a.UserId).IsRequired(false);
            builder.Property(a => a.Action).IsRequired().HasMaxLength(100);
            builder.Property(a => a.EntityType).IsRequired().HasMaxLength(100);
            builder.Property(a => a.EntityId).IsRequired();
            builder.Property(a => a.Details).HasMaxLength(4000);
            builder.Property(a => a.Timestamp).IsRequired();
            builder.Property(a => a.IpAddress).HasMaxLength(50);

            builder.HasIndex(a => a.Timestamp)
                .HasDatabaseName("IX_AuditLogs_Timestamp")
                .IsDescending();
            builder.HasIndex(a => a.UserId)
                .HasDatabaseName("IX_AuditLogs_UserId");
            builder.HasIndex(a => a.Action)
                .HasDatabaseName("IX_AuditLogs_Action");
            builder.HasIndex(a => a.EntityType)
                .HasDatabaseName("IX_AuditLogs_EntityType");
            builder.HasIndex(a => a.EntityId)
                .HasDatabaseName("IX_AuditLogs_EntityId");
        }
    }
}
