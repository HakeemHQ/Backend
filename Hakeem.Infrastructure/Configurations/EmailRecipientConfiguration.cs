using Hakeem.Domain.Entities.NotificationsEntites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class EmailRecipientConfiguration : IEntityTypeConfiguration<EmailRecipient>
{
    public void Configure(EntityTypeBuilder<EmailRecipient> builder)
    {
        builder.ToTable("EmailRecipients");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .UseIdentityColumn();

        builder.Property(r => r.RecipientKey)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(r => r.EmailAddress)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(r => r.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        // Non-unique — multiple active rows per key is intentional
        builder.HasIndex(r => r.RecipientKey)
            .HasDatabaseName("IX_EmailRecipients_RecipientKey");
    }
}
