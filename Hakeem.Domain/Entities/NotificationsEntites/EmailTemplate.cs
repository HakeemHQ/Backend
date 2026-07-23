using Hakeem.Domain.Enums.Notifications;

namespace Hakeem.Domain.Entities.NotificationsEntites;

/// <summary>
/// Stores bilingual HTML email templates with optional department-recipient routing.
/// When <see cref="RecipientKey"/> is set, the handler resolves addresses from <see cref="EmailRecipient"/>.
/// When null, the handler uses the dynamic address from the outbox event payload.
/// </summary>
public class EmailTemplate
{
    public int Id { get; set; }

    /// <summary>Unique enum key linking this row to a handler. Stored as int.</summary>
    public EmailTemplateKey TemplateKey { get; set; }

    /// <summary>Human-readable name, e.g. "New Account Creation - invitation Email".</summary>
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>Combined bilingual subject separated by " | ".</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>Bilingual HTML body with {{Placeholder}} tokens.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// When set, recipients are resolved from <see cref="EmailRecipient"/> rows matching this key.
    /// When null, the recipient address is taken from the outbox event payload at runtime.
    /// </summary>
    public EmailRecipientKey? RecipientKey { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
