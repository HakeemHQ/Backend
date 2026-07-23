using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class MedicalCvVersionConfiguration : IEntityTypeConfiguration<MedicalCvVersion>
{
    public void Configure(EntityTypeBuilder<MedicalCvVersion> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasMany(m => m.SharedCvLinks)
               .WithOne(s => s.MedicalCvVersion)
               .HasForeignKey(s => s.MedicalCvVersionId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
