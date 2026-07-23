using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class ExtractedFieldConfiguration : IEntityTypeConfiguration<ExtractedField>
{
    public void Configure(EntityTypeBuilder<ExtractedField> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.FieldReview)
               .WithOne(f => f.ExtractedField)
               .HasForeignKey<FieldReview>(f => f.ExtractedFieldId)
               .OnDelete(DeleteBehavior.Cascade);
        
        builder.Property(e => e.Confidence).HasColumnType("decimal(18,4)");
    }
}
