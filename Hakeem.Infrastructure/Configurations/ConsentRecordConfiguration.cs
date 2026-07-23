using Hakeem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class ConsentRecordConfiguration : IEntityTypeConfiguration<ConsentRecord>
{
    public void Configure(EntityTypeBuilder<ConsentRecord> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasMany(c => c.SharedCvLinks)
               .WithOne(s => s.ConsentRecord)
               .HasForeignKey(s => s.ConsentRecordId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
