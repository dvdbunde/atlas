using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entities = ATLAS.Domain.Entities;
using ATLAS.Domain.ValueObjects;

namespace ATLAS.Infrastructure.Data.Configurations
{
    public class ApplicationConfiguration : IEntityTypeConfiguration<Entities.Application>
    {
        public void Configure(EntityTypeBuilder<Entities.Application> builder)
        {
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Id).HasDefaultValueSql("NEWID()");
            builder.Property(a => a.ApplicationNumber).IsRequired().HasMaxLength(50);
            builder.Property(a => a.PermitTypeId).IsRequired();
            builder.Property(a => a.Status).IsRequired();
            builder.Property(a => a.CitizenNotes).HasMaxLength(2000);
            builder.Property(a => a.OfficerNotes).HasMaxLength(2000);
            builder.Property(a => a.SubmittedDate).IsRequired(false);
            builder.Property(a => a.ReviewedDate).IsRequired(false);
            builder.HasOne<Entities.PermitType>().WithMany().HasForeignKey("PermitTypeId").IsRequired();
            builder.HasOne<Entities.User>().WithMany().HasForeignKey(a => a.CitizenId).IsRequired();
            
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
                review.Property(r => r.ReasonCode).HasMaxLength(50).IsRequired(false);
                review.Property(r => r.Comments).HasMaxLength(2000);
                review.HasOne<Entities.User>().WithMany().HasForeignKey(r => r.OfficerId).IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
