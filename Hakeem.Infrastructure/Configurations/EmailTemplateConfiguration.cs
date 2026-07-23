using Hakeem.Domain.Entities.NotificationsEntites;
using Hakeem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.ToTable("EmailTemplates");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .UseIdentityColumn();

        builder.Property(t => t.TemplateKey)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(t => t.TemplateName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(t => t.Subject)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(t => t.Body)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        // Nullable — null means dynamic address from event payload
        builder.Property(t => t.RecipientKey)
            .HasConversion<int?>();

        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.UpdatedAt);

        // One template per key
        builder.HasIndex(t => t.TemplateKey)
            .IsUnique()
            .HasDatabaseName("IX_EmailTemplates_TemplateKey");
    }
}
