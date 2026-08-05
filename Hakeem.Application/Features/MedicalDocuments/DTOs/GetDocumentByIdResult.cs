namespace Hakeem.Application.Features.MedicalDocuments.DTOs;

public sealed record GetDocumentByIdResult(
    Guid DocumentId,
    string DocumentType,
    string Title,
    DateOnly DocumentDate,
    string ExtractionStatus,
    string? FailureCode,
    string DocumentPath);
