using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Access;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public sealed class DoctorPatientAccessConfiguration
    : IEntityTypeConfiguration<DoctorPatientAccess>
{
    public void Configure(EntityTypeBuilder<DoctorPatientAccess> builder)
    {
        builder.HasKey(access => access.Id);

        builder.Property(access => access.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(DoctorPatientAccessStatus.Active);

        builder.HasIndex(access => access.PatientAccessRequestId)
            .IsUnique();

        builder.HasIndex(access => new
        {
            access.DoctorProfileId,
            access.PatientProfileId,
            access.Status
        });

        builder.HasIndex(access => new
        {
            access.DoctorProfileId,
            access.PatientProfileId
        })
            .IsUnique()
            .HasFilter("[Status] = 'Active'");

        builder.HasOne(access => access.PatientAccessRequest)
            .WithOne(request => request.Access)
            .HasForeignKey<DoctorPatientAccess>(access => access.PatientAccessRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(access => access.Doctor)
            .WithMany(doctor => doctor.PatientAccesses)
            .HasForeignKey(access => access.DoctorProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(access => access.Patient)
            .WithMany(patient => patient.DoctorAccesses)
            .HasForeignKey(access => access.PatientProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
