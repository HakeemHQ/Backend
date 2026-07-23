using Hakeem.Domain.Enums.Notifications;

namespace Hakeem.Domain.Entities.NotificationsEntites;

/// <summary>
/// Maps a department key to one or more email addresses.
/// Multiple active rows for the same <see cref="RecipientKey"/> are each sent a separate email.
/// </summary>
public class EmailRecipient
{
    public int Id { get; set; }

    /// <summary>Department key, e.g. AntiFraudHakeem. Multiple rows per key are allowed.</summary>
    public EmailRecipientKey RecipientKey { get; set; }

    public string EmailAddress { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
