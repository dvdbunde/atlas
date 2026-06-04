using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ATLAS.Domain.Entities;
using ATLAS.Domain.ValueObjects;

namespace ATLAS.Infrastructure.Data.Configurations
{
    public class ApplicationConfiguration : IEntityTypeConfiguration<Application>
    {
        public void Configure(EntityTypeBuilder<Application> builder)
        {
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Id).HasDefaultValueSql("NEWID()");
            builder.Property(a => a.ApplicationNumber).IsRequired().HasMaxLength(50);
            builder.Property(a => a.CitizenNotes).HasMaxLength(2000);
            builder.Property(a => a.OfficerNotes).HasMaxLength(2000);
            builder.HasOne<PermitType>().WithMany().HasForeignKey("PermitTypeId").IsRequired();
            builder.HasOne<User>().WithMany().HasForeignKey(a => a.CitizenId).IsRequired();
            
            // Map value objects as owned entities
            builder.OwnsMany(a => a.Documents, doc =>
            {
                doc.HasKey(d => d.Id);
                doc.Property(d => d.FileName).IsRequired().HasMaxLength(255);
                doc.Property(d => d.ContentType).IsRequired().HasMaxLength(100);
                doc.Property(d => d.BlobUrl).IsRequired().HasMaxLength(500);
            });
            
            builder.OwnsMany(a => a.Reviews, review =>
            {
                review.HasKey(r => r.Id);
                review.Property(r => r.Decision).IsRequired();
                review.Property(r => r.ReasonCode).HasMaxLength(50);
                review.Property(r => r.Comments).HasMaxLength(2000);
                review.HasOne<User>().WithMany().HasForeignKey(r => r.OfficerId).IsRequired();
            });
        }
    }
}
