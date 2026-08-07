namespace Hakeem.Domain.DomainEvents.Outbox;

public sealed class CreateMedicalCvRequest : OutboxEventBase
{
    public Guid MedicalCvVersionId { get; set; }
}
