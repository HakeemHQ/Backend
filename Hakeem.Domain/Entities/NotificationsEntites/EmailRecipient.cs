using Hakeem.Domain.Enums.Notifications;

namespace Hakeem.Domain.Entities.NotificationsEntites;

public class EmailRecipient
{
    public int Id { get; set; }

    public EmailRecipientKey RecipientKey { get; set; }

    public string EmailAddress { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
