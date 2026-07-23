using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasOne(u => u.PatientProfile)
               .WithOne(p => p.User)
               .HasForeignKey<PatientProfile>(p => p.UserId);

        builder.HasMany(u => u.AuditLogs)
               .WithOne(a => a.ActorUser)
               .HasForeignKey(a => a.ActorUserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.RefreshTokens)
               .WithOne(r => r.User)
               .HasForeignKey(r => r.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
