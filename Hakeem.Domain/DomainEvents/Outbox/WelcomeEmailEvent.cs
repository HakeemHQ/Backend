namespace Hakeem.Domain.DomainEvents.Outbox;

public class WelcomeEmailEvent : OutboxEventBase
{
    public string RecipientEmail { get; set; } = string.Empty;
    public string? UserNameEn { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
