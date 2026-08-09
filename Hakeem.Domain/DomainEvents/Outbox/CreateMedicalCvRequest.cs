namespace Hakeem.Domain.DomainEvents.Outbox;

public sealed class CreateMedicalCvRequest : OutboxEventBase
{
    public Guid MedicalCvVersionId { get; set; }
    public string Language { get; set; } = "en";
}
