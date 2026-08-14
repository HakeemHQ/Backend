namespace Hakeem.Domain.DomainEvents.Outbox;

public sealed class PatientAccessRequestedEvent : OutboxEventBase
{
    public Guid RequestId { get; set; }
    public Guid PatientUserId { get; set; }
}
