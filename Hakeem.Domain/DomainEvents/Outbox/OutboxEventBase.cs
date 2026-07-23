namespace Hakeem.Domain.DomainEvents.Outbox;


public abstract class OutboxEventBase
{
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
}
