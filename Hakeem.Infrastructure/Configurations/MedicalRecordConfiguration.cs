using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class MedicalRecordConfiguration : IEntityTypeConfiguration<MedicalRecord>
{
    public void Configure(EntityTypeBuilder<MedicalRecord> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasMany(m => m.FieldReviews)
               .WithOne(f => f.MedicalRecord)
               .HasForeignKey(f => f.MedicalRecordId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(m => m.SourceReferences)
               .WithOne(s => s.MedicalRecord)
               .HasForeignKey(s => s.MedicalRecordId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.SummarizedInCvVersions)
               .WithMany(v => v.SummarizedRecords)
               .UsingEntity(
                   "MedicalRecordMedicalCvVersion",
                   l => l.HasOne(typeof(MedicalCvVersion)).WithMany().HasForeignKey("SummarizedInCvVersionsId").OnDelete(DeleteBehavior.Restrict),
                   r => r.HasOne(typeof(MedicalRecord)).WithMany().HasForeignKey("SummarizedRecordsId").OnDelete(DeleteBehavior.Restrict));

        builder.HasMany(m => m.Reminders)
               .WithOne(r => r.MedicalRecord)
               .HasForeignKey(r => r.MedicalRecordId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
