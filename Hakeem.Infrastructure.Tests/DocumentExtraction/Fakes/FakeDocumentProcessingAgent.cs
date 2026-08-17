using Hakeem.Application.Features.MedicalDocuments.DTOs;
using Hakeem.Application.Interfaces.Agents;

namespace Hakeem.Infrastructure.Tests.DocumentExtraction.Fakes;

internal sealed class FakeDocumentProcessingAgent : IDocumentProcessingAgent
{
    private readonly DocumentProcessingResult _result;

    public FakeDocumentProcessingAgent(DocumentExtractionResult extraction)
        : this(DocumentProcessingResult.Medical(
            new MedicalDocumentClassification(
                true,
                extraction.DocumentType,
                0.99,
                null),
            extraction))
    {
    }

    public FakeDocumentProcessingAgent(DocumentProcessingResult result)
    {
        _result = result;
    }

    public int CallCount { get; private set; }

    public Task<DocumentProcessingResult> ProcessAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CallCount++;

        return Task.FromResult(_result);
    }
}
