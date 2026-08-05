using Hakeem.Application.Abstractions;
using Hakeem.Application.Interfaces.Processors;
using Hakeem.Domain.DomainEvents.Outbox;

namespace Hakeem.Application.Projections.RagHandlers;

public sealed class MedicalRecordFieldIndexedEventHandler(
    IMedicalRecordFieldIndexProcessor processor)
    : IOutboxEventHandler<MedicalRecordFieldIndexedEvent>
{
    public Task HandleAsync(
        MedicalRecordFieldIndexedEvent @event,
        CancellationToken cancellationToken)
    {
        return processor.ProcessAsync(
            @event.MedicalRecordFieldId,
            cancellationToken);
    }
}
