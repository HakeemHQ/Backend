using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class MedicalCvVersionConfiguration : IEntityTypeConfiguration<MedicalCvVersion>
{
    public void Configure(EntityTypeBuilder<MedicalCvVersion> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.MedicalCvId, e.VersionNumber })
               .IsUnique();

        builder.Property(e => e.ScopeType)
               .HasConversion<string>()
               .HasMaxLength(20)
               .IsRequired();

        builder.Property(e => e.Focus)
               .HasMaxLength(200);

        builder.Property(e => e.Status)
               .HasConversion<string>()
               .HasMaxLength(20)
               .IsRequired();

        builder.Property(e => e.PdfFileKey)
               .HasMaxLength(500)
               .IsRequired();

        builder.HasMany(m => m.SharedCvLinks)
               .WithOne(s => s.MedicalCvVersion)
               .HasForeignKey(s => s.MedicalCvVersionId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
