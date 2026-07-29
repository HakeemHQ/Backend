using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class ExtractedFieldConfiguration : IEntityTypeConfiguration<ExtractedField>
{
    public void Configure(EntityTypeBuilder<ExtractedField> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.FieldName)
               .IsRequired();

        builder.Property(e => e.ExtractedValue)
               .IsRequired();

        builder.Property(e => e.EvidenceText)
               .IsRequired();

        builder.Property(e => e.Issues)
               .IsRequired();

        builder.Property(e => e.Confidence)
               .HasColumnType("decimal(18,4)");

        builder.HasOne(e => e.FieldReview)
               .WithOne(f => f.ExtractedField)
               .HasForeignKey<FieldReview>(f => f.ExtractedFieldId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
