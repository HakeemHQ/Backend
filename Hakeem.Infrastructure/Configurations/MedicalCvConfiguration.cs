using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class MedicalCvConfiguration : IEntityTypeConfiguration<MedicalCv>
{
    public void Configure(EntityTypeBuilder<MedicalCv> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
               .HasMaxLength(200)
               .IsRequired();

        builder.Property(e => e.ScopeType)
               .HasConversion<string>()
               .HasMaxLength(20)
               .IsRequired();

        builder.Property(e => e.Focus)
               .HasMaxLength(200);

        builder.HasIndex(e => new
               {
                   e.PatientId,
                   e.ScopeType,
                   e.Focus
               })
               .HasFilter(null)
               .IsUnique();

        builder.ToTable(table => table.HasCheckConstraint(
            "CK_MedicalCvs_ScopeType_Focus",
            "([ScopeType] = 'Full' AND [Focus] IS NULL) OR " +
            "([ScopeType] = 'Focused' AND [Focus] IS NOT NULL AND LEN(LTRIM(RTRIM([Focus]))) > 0)"));

        builder.HasMany(m => m.Versions)
               .WithOne(v => v.MedicalCv)
               .HasForeignKey(v => v.MedicalCvId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
