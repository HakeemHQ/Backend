namespace Hakeem.Application.Features.MedicalDocuments.DTOs;

public sealed class MedicalDocumentDto
{
    public Guid DocumentId { get; init; }

    public string DocumentType { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public DateOnly DocumentDate { get; init; }

    public string ExtractionStatus { get; init; } = string.Empty;
}
