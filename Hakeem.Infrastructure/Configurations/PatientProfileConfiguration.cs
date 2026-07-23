using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class PatientProfileConfiguration : IEntityTypeConfiguration<PatientProfile>
{
    public void Configure(EntityTypeBuilder<PatientProfile> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.HasMany(p => p.MedicalDocuments)
               .WithOne(d => d.PatientProfile)
               .HasForeignKey(d => d.PatientProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.MedicalRecords)
               .WithOne(m => m.PatientProfile)
               .HasForeignKey(m => m.PatientProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.MedicalCvs)
               .WithOne(c => c.PatientProfile)
               .HasForeignKey(c => c.PatientProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Reminders)
               .WithOne(r => r.PatientProfile)
               .HasForeignKey(r => r.PatientProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.ConsentRecords)
               .WithOne(c => c.PatientProfile)
               .HasForeignKey(c => c.PatientProfileId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.AuditLogs)
               .WithOne(a => a.PatientProfile)
               .HasForeignKey(a => a.PatientProfileId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
