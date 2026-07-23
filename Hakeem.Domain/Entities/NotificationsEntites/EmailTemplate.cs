using Hakeem.Domain.Enums.Notifications;

namespace Hakeem.Domain.Entities.NotificationsEntites;


public class EmailTemplate
{
    public int Id { get; set; }

    public EmailTemplateKey TemplateKey { get; set; }

    public string TemplateName { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;


    public EmailRecipientKey? RecipientKey { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
