namespace Hakeem.Domain.DomainEvents.Outbox;

public sealed class DocumentExtractionRequestedEvent : OutboxEventBase
{
    public Guid DocumentId { get; set; }
}
