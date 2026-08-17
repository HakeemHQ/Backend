using Hakeem.Application.Projections.DocumentExtractionHandlers;
using Hakeem.Domain.DomainEvents.Outbox;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Documents;
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

    [Fact]
    public async Task HandleFailureAsync_ProcessingDocument_MarksExtractionFailed()
    {
        var document = new MedicalDocument
        {
            Id = Guid.NewGuid()
        };
        document.StartExtraction();
        var handler = new DocumentExtractionRequestedEventFailureHandler(
            new FakeMedicalDocumentRepository(document));

        await handler.HandleFailureAsync(
            new DocumentExtractionRequestedEvent
            {
                DocumentId = document.Id
            },
            CancellationToken.None);

        Assert.Equal(ExtractionStatus.Failed, document.ExtractionStatus);
        Assert.Equal("Document.ExtractionFailed", document.FailureCode);
    }

    [Fact]
    public async Task HandleFailureAsync_QueuedDocument_LeavesStatusUnchanged()
    {
        var document = new MedicalDocument
        {
            Id = Guid.NewGuid()
        };
        var handler = new DocumentExtractionRequestedEventFailureHandler(
            new FakeMedicalDocumentRepository(document));

        await handler.HandleFailureAsync(
            new DocumentExtractionRequestedEvent
            {
                DocumentId = document.Id
            },
            CancellationToken.None);

        Assert.Equal(ExtractionStatus.Queued, document.ExtractionStatus);
        Assert.Null(document.FailureCode);
    }

    [Fact]
    public async Task HandleFailureAsync_RejectedDocument_RemainsDistinctFromTechnicalFailure()
    {
        var document = new MedicalDocument
        {
            Id = Guid.NewGuid()
        };
        document.StartExtraction();
        document.RejectExtraction("Document.NotMedical");
        var handler = new DocumentExtractionRequestedEventFailureHandler(
            new FakeMedicalDocumentRepository(document));

        await handler.HandleFailureAsync(
            new DocumentExtractionRequestedEvent
            {
                DocumentId = document.Id
            },
            CancellationToken.None);

        Assert.Equal(ExtractionStatus.Rejected, document.ExtractionStatus);
        Assert.Equal("Document.NotMedical", document.FailureCode);
    }
}
