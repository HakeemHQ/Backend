using Hakeem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Hakeem.Domain.Entities.NotificationsEntites;
using Hakeem.Domain.Enums.Notifications;

namespace Hakeem.Infrastructure.Configurations;

public class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
{
    public void Configure(EntityTypeBuilder<OutboxEvent> builder)
    {
        builder.ToTable("OutboxEvents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Payload)
            .IsRequired();

        builder.Property(e => e.Status)
            .IsRequired()
            .HasDefaultValue(OutboxEventStatus.Pending);

        builder.Property(e => e.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.MaxRetries)
            .IsRequired()
            .HasDefaultValue(3);

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(4000);

        builder.Property(e => e.IdempotencyKey)
            .HasMaxLength(256);

        // Composite index for efficient polling: WHERE Status = Pending AND NextRetryAt <= NOW
        builder.HasIndex(e => new { e.Status, e.NextRetryAt })
            .HasDatabaseName("IX_OutboxEvents_Status_NextRetryAt");

        // Unique filtered index on IdempotencyKey (only non-null values)
        builder.HasIndex(e => e.IdempotencyKey)
            .IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL")
            .HasDatabaseName("IX_OutboxEvents_IdempotencyKey");
    }
}
