using Hakeem.Application.Abstractions;
using Hakeem.Application.Interfaces.Processors;
using Hakeem.Domain.DomainEvents.Outbox;

namespace Hakeem.Application.Projections.RagHandlers;

public sealed class MedicalRecordIndexedEventHandler(
    IMedicalRecordIndexProcessor processor)
    : IOutboxEventHandler<MedicalRecordIndexedEvent>
{
    public Task HandleAsync(
        MedicalRecordIndexedEvent @event,
        CancellationToken cancellationToken)
    {
        return processor.ProcessAsync(
            @event.MedicalRecordId,
            cancellationToken);
    }
}
