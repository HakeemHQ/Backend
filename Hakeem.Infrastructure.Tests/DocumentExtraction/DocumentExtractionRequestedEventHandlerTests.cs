using Hakeem.Application.Projections.DocumentExtractionHandlers;
using Hakeem.Domain.DomainEvents.Outbox;
using Hakeem.Infrastructure.Tests.DocumentExtraction.Fakes;

namespace Hakeem.Infrastructure.Tests.DocumentExtraction;

public sealed class DocumentExtractionRequestedEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_InvokesProcessorWithEventDocumentId()
    {
        var processor = new FakeDocumentExtractionProcessor();
        var handler =
            new DocumentExtractionRequestedEventHandler(processor);
        var documentId = Guid.NewGuid();
        var @event = new DocumentExtractionRequestedEvent
        {
            DocumentId = documentId
        };
        using var cancellationSource =
            new CancellationTokenSource();

        await handler.HandleAsync(
            @event,
            cancellationSource.Token);

        Assert.Equal(1, processor.CallCount);
        Assert.Equal(documentId, processor.ReceivedDocumentId);
        Assert.Equal(
            cancellationSource.Token,
            processor.ReceivedCancellationToken);
    }
}
