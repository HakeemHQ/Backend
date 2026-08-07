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

        builder.HasMany(m => m.Versions)
               .WithOne(v => v.MedicalCv)
               .HasForeignKey(v => v.MedicalCvId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
