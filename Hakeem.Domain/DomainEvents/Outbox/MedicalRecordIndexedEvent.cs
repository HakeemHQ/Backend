namespace Hakeem.Domain.DomainEvents.Outbox;

public sealed class MedicalRecordIndexedEvent : OutboxEventBase
{
    public Guid MedicalRecordId { get; set; }
}
