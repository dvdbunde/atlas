using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ATLAS.Domain.Entities;
using ATLAS.Domain.ValueObjects;
using System.Text.Json;

namespace ATLAS.Infrastructure.Data.Configurations
{
    public class PermitTypeConfiguration : IEntityTypeConfiguration<PermitType>
    {
        public void Configure(EntityTypeBuilder<PermitType> builder)
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).HasDefaultValueSql("NEWID()");
            builder.Property(p => p.Name).IsRequired().HasMaxLength(100);
            builder.Property(p => p.Description).HasMaxLength(500);
            builder.Property(p => p.IsActive).HasDefaultValue(true);
            
            // Map value objects as owned entities
            builder.OwnsMany(p => p.Fields, field =>
            {
                field.Property(f => f.Name).IsRequired().HasMaxLength(100);
                field.Property(f => f.Type).IsRequired();
                field.Property(f => f.IsRequired).HasDefaultValue(false);
                field.Property(f => f.DefaultValue).HasMaxLength(255);         

                field.Property<List<string>>("_options")
                    .HasColumnName("Options")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new())
                    .HasColumnType("nvarchar(max)");
                
                      
            });
            
            builder.OwnsMany(p => p.DocumentRequirements, doc =>
            {
                doc.Property(d => d.DocumentType).IsRequired().HasMaxLength(100);
                doc.Property(d => d.IsRequired).HasDefaultValue(false);
                doc.Property(d => d.MaxFileSizeBytes).HasDefaultValue(26214400); // 25MB
            });          
        }
    }
}
