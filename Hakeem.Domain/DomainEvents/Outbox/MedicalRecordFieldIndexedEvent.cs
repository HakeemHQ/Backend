namespace Hakeem.Domain.DomainEvents.Outbox;

public sealed class MedicalRecordFieldIndexedEvent : OutboxEventBase
{
    public Guid MedicalRecordFieldId { get; set; }
}
