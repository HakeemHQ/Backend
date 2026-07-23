using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class MedicalDocumentConfiguration : IEntityTypeConfiguration<MedicalDocument>
{
    public void Configure(EntityTypeBuilder<MedicalDocument> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.HasMany(d => d.ExtractedFields)
               .WithOne(e => e.MedicalDocument)
               .HasForeignKey(e => e.DocumentId)
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
