using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Token).HasMaxLength(256).IsRequired();
        builder.Property(e => e.JwtId).HasMaxLength(64).IsRequired();
        builder.HasIndex(e => e.Token).IsUnique();
        builder.HasIndex(e => e.JwtId).IsUnique();
    }
}
