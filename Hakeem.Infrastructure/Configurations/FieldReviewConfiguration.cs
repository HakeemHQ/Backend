using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class FieldReviewConfiguration : IEntityTypeConfiguration<FieldReview>
{
    public void Configure(EntityTypeBuilder<FieldReview> builder)
    {
        builder.HasKey(e => e.Id);
    }
}
