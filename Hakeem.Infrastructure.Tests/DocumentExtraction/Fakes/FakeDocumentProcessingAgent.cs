using Hakeem.Application.Features.MedicalDocuments.DTOs;
using Hakeem.Application.Interfaces.Agents;

namespace Hakeem.Infrastructure.Tests.DocumentExtraction.Fakes;

internal sealed class FakeDocumentProcessingAgent(
    DocumentExtractionResult result)
    : IDocumentProcessingAgent
{
    public int CallCount { get; private set; }

    public Task<DocumentExtractionResult> ProcessAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CallCount++;

        return Task.FromResult(result);
    }
}
