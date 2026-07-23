using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class SharedCvLinkConfiguration : IEntityTypeConfiguration<SharedCvLink>
{
    public void Configure(EntityTypeBuilder<SharedCvLink> builder)
    {
        builder.HasKey(e => e.Id);
    }
}
