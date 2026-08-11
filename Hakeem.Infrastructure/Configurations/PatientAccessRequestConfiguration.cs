using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Access;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public sealed class PatientAccessRequestConfiguration
    : IEntityTypeConfiguration<PatientAccessRequest>
{
    public void Configure(EntityTypeBuilder<PatientAccessRequest> builder)
    {
        builder.HasKey(request => request.Id);

        builder.Property(request => request.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(PatientAccessRequestStatus.Pending);

        builder.Property(request => request.CodeHash)
            .HasMaxLength(128);

        builder.HasIndex(request => request.CodeHash)
            .IsUnique()
            .HasFilter("[CodeHash] IS NOT NULL");

        builder.HasIndex(request => new
        {
            request.PatientProfileId,
            request.Status,
            request.RequestedAt
        });

        builder.HasIndex(request => new
        {
            request.DoctorProfileId,
            request.Status,
            request.RequestedAt
        });

        builder.HasOne(request => request.Doctor)
            .WithMany(doctor => doctor.PatientAccessRequests)
            .HasForeignKey(request => request.DoctorProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(request => request.Patient)
            .WithMany(patient => patient.DoctorAccessRequests)
            .HasForeignKey(request => request.PatientProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
