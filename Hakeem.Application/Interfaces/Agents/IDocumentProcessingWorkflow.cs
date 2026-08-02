using Hakeem.Application.Features.MedicalDocuments.DTOs;

namespace Hakeem.Application.Interfaces.Agents;

public interface IDocumentProcessingWorkflow
{
    Task<DocumentExtractionResult> ProcessAsync(
        Guid documentId,
        CancellationToken cancellationToken);
}
