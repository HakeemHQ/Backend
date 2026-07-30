using Hakeem.Application.Features.MedicalDocuments.DTOs;

namespace Hakeem.Application.Interfaces.Agents;

public interface IDocumentProcessingAgent
{
    Task<DocumentExtractionResult> ProcessAsync(
        Guid documentId,
        CancellationToken cancellationToken);
}
