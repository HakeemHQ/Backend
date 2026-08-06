using Hakeem.Application.Interfaces.Rag;
using Hakeem.Application.Repositories.Notifications;
using Hakeem.Domain.DomainEvents.Outbox;

namespace Hakeem.Application.Services.Rag;

public sealed class MedicalRecordFieldIndexOutbox(
    IOutboxEventRepository outboxEventRepository)
    : IMedicalRecordFieldIndexOutbox
{
    public void EnqueueIndexing(Guid medicalRecordFieldId)
    {
        outboxEventRepository.Add(
            new MedicalRecordFieldIndexedEvent
            {
                MedicalRecordFieldId = medicalRecordFieldId
            },
            BuildIdempotencyKey(medicalRecordFieldId));
    }

    public void EnqueueIndexing(IEnumerable<Guid> medicalRecordFieldIds)
    {
        foreach (var medicalRecordFieldId in medicalRecordFieldIds)
        {
            EnqueueIndexing(medicalRecordFieldId);
        }
    }

    private static string BuildIdempotencyKey(Guid medicalRecordFieldId)
        => $"medical-record-field-index:{medicalRecordFieldId}";
}
