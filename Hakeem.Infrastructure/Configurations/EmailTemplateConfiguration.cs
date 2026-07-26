using Hakeem.Domain.Entities.NotificationsEntites;
using Hakeem.Domain.Enums.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hakeem.Infrastructure.Configurations;

public sealed class EmailTemplateConfiguration
    : IEntityTypeConfiguration<EmailTemplate>
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

        // Null means the recipient address comes dynamically
        // from the outbox event payload.
        builder.Property(t => t.RecipientKey)
            .HasConversion<int?>();

        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.UpdatedAt);

        // Only one template is allowed for each template key.
        builder.HasIndex(t => t.TemplateKey)
            .IsUnique()
            .HasDatabaseName("IX_EmailTemplates_TemplateKey");

        // Required application email templates.
        builder.HasData(
            new EmailTemplate
            {
                Id = 1,
                TemplateKey = EmailTemplateKey.PasswordReset,
                TemplateName = "PasswordReset",
                Subject = "Reset your Hakeem password",
                Body = """
                    <p>Hello {{UserName}},</p>

                    <p>We received a request to reset your Hakeem password.</p>

                    <p>
                        <a href="{{ResetLink}}">Reset your password</a>
                    </p>

                    <p>
                        If you did not request a password reset,
                        you can safely ignore this email.
                    </p>
                    """,
                RecipientKey = null,
                IsActive = true,
                CreatedAt = new DateTime(
                    2026,
                    7,
                    26,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc),
                UpdatedAt = null
            });
    }
}
