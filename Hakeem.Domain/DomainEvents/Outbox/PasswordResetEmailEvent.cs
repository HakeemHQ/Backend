namespace Hakeem.Domain.DomainEvents.Outbox;

public class PasswordResetEmailEvent : OutboxEventBase
{
    public string RecipientEmail { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string ResetLink { get; set; } = string.Empty;
}
