using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class ExtractedItemConfiguration : IEntityTypeConfiguration<ExtractedItem>
{
    public void Configure(EntityTypeBuilder<ExtractedItem> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ItemType)
               .IsRequired();

        builder.HasIndex(e => new { e.MedicalDocumentId, e.SequenceNumber })
               .IsUnique();

        builder.HasMany(e => e.ExtractedFields)
               .WithOne(f => f.ExtractedItem)
               .HasForeignKey(f => f.ExtractedItemId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
