using Hakeem.Application.Interfaces.Processors;

namespace Hakeem.Infrastructure.Tests.DocumentExtraction.Fakes;

internal sealed class FakeDocumentExtractionProcessor
    : IDocumentExtractionProcessor
{
    public int CallCount { get; private set; }
    public Guid ReceivedDocumentId { get; private set; }
    public CancellationToken ReceivedCancellationToken { get; private set; }

    public Task ProcessAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        CallCount++;
        ReceivedDocumentId = documentId;
        ReceivedCancellationToken = cancellationToken;

        return Task.CompletedTask;
    }
}
