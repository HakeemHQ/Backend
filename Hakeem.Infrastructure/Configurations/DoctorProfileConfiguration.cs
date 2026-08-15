using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public sealed class DoctorProfileConfiguration : IEntityTypeConfiguration<DoctorProfile>
{
    public void Configure(EntityTypeBuilder<DoctorProfile> builder)
    {
        builder.HasKey(profile => profile.Id);

        builder.Property(profile => profile.Specialty)
            .HasMaxLength(200);

        builder.Property(profile => profile.LicenseNumber)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(profile => profile.LicenseNumber)
            .IsUnique();

        builder.HasIndex(profile => profile.UserId)
            .IsUnique();

        builder.HasOne(profile => profile.User)
            .WithOne(user => user.DoctorProfile)
            .HasForeignKey<DoctorProfile>(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
