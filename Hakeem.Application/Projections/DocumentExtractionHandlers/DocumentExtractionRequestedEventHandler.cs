using Hakeem.Application.Abstractions;
using Hakeem.Application.Interfaces.Processors;
using Hakeem.Domain.DomainEvents.Outbox;

namespace Hakeem.Application.Projections.DocumentExtractionHandlers;

public sealed class DocumentExtractionRequestedEventHandler(
    IDocumentExtractionProcessor processor)
    : IOutboxEventHandler<DocumentExtractionRequestedEvent>
{
    public Task HandleAsync(
        DocumentExtractionRequestedEvent @event,
        CancellationToken cancellationToken)
    {
        return processor.ProcessAsync(
            @event.DocumentId,
            cancellationToken);
    }
}
