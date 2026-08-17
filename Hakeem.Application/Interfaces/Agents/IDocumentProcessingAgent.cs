using Hakeem.Application.Features.MedicalDocuments.DTOs;

namespace Hakeem.Application.Interfaces.Agents;

public interface IDocumentProcessingAgent
{
    Task<DocumentProcessingResult> ProcessAsync(
        Guid documentId,
        CancellationToken cancellationToken);
}

public sealed record DocumentProcessingResult(
    MedicalDocumentClassification Classification,
    DocumentExtractionResult? Extraction)
{
    public static DocumentProcessingResult Medical(
        MedicalDocumentClassification classification,
        DocumentExtractionResult extraction) =>
        new(classification, extraction);

    public static DocumentProcessingResult Rejected(
        MedicalDocumentClassification classification) =>
        new(classification, null);
}
