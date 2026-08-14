using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public sealed class PushDeviceConfiguration : IEntityTypeConfiguration<PushDevice>
{
    public void Configure(EntityTypeBuilder<PushDevice> builder)
    {
        builder.HasKey(device => device.Id);

        builder.Property(device => device.ExpoPushToken)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(device => device.Platform)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(device => device.Language)
            .IsRequired()
            .HasMaxLength(5)
            .HasDefaultValue("en");

        builder.Property(device => device.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(device => device.ExpoPushToken)
            .IsUnique();

        builder.HasIndex(device => new { device.UserId, device.IsActive });

        builder.HasOne(device => device.User)
            .WithMany(user => user.PushDevices)
            .HasForeignKey(device => device.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
