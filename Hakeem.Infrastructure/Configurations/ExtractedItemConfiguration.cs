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

        builder.Property(e => e.ReviewStatus)
               .IsRequired();

        builder.Property(e => e.ReviewedByUserId)
               .IsRequired(false);

        builder.HasIndex(e => new
               {
                   e.MedicalDocumentId,
                   e.ItemType,
                   e.SequenceNumber
               })
               .IsUnique();

        builder.HasMany(e => e.ExtractedFields)
               .WithOne(f => f.ExtractedItem)
               .HasForeignKey(f => f.ExtractedItemId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.MedicalRecord)
               .WithOne(record => record.SourceExtractedItem)
               .HasForeignKey<MedicalRecord>(
                 record => record.SourceExtractedItemId)
               .OnDelete(DeleteBehavior.Restrict);

    }
}
