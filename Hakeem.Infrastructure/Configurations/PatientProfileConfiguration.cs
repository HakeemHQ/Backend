using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class PatientProfileConfiguration : IEntityTypeConfiguration<PatientProfile>
{
    public void Configure(EntityTypeBuilder<PatientProfile> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(profile => profile.PatientCode)
            .HasMaxLength(8);
        builder.HasIndex(profile => profile.PatientCode)
            .IsUnique();

        builder.Property(profile => profile.NationalId)
            .HasMaxLength(14);

        builder.Property(profile => profile.IdentityVerificationStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(IdentityVerificationStatus.Pending);

        builder.Property(profile => profile.VerifiedNationalId)
            .HasMaxLength(14);
        builder.HasIndex(profile => profile.VerifiedNationalId)
            .IsUnique()
            .HasFilter("[VerifiedNationalId] IS NOT NULL");

        builder.HasOne(profile => profile.VerifiedByDoctor)
            .WithMany(doctor => doctor.VerifiedPatients)
            .HasForeignKey(profile => profile.VerifiedByDoctorId)
            .OnDelete(DeleteBehavior.Restrict);
        
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
               .HasForeignKey(c => c.PatientId)
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
