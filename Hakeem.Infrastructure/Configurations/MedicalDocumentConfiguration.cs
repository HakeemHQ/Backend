using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class MedicalDocumentConfiguration : IEntityTypeConfiguration<MedicalDocument>
{
    public void Configure(EntityTypeBuilder<MedicalDocument> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(d => d.ExtractionStatus)
               .HasConversion<string>()
               .HasMaxLength(20)
               .IsRequired();

        builder.Property(d => d.FailureCode)
               .HasMaxLength(100);

        builder.HasMany(d => d.ExtractedItems)
               .WithOne(i => i.MedicalDocument)
               .HasForeignKey(i => i.MedicalDocumentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.SourceReferences)
               .WithOne(s => s.MedicalDocument)
               .HasForeignKey(s => s.DocumentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.DocumentChunks)
               .WithOne(c => c.MedicalDocument)
               .HasForeignKey(c => c.DocumentId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
