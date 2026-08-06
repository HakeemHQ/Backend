using Hakeem.Application.Interfaces.Rag;
using Hakeem.Application.Repositories.Notifications;
using Hakeem.Domain.DomainEvents.Outbox;

namespace Hakeem.Application.Services.Rag;

public sealed class MedicalRecordIndexOutbox(
    IOutboxEventRepository outboxEventRepository)
    : IMedicalRecordIndexOutbox
{
    public void EnqueueIndexing(Guid medicalRecordId)
    {
        outboxEventRepository.Add(
            new MedicalRecordIndexedEvent
            {
                MedicalRecordId = medicalRecordId
            },
            $"medical-record-index:{medicalRecordId}");
    }
}
