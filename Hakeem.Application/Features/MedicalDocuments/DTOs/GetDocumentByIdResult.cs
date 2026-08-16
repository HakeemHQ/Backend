namespace Hakeem.Application.Features.MedicalDocuments.DTOs;

public sealed record GetDocumentByIdResult(
    Guid DocumentId,
    string DocumentType,
    string Title,
    DateOnly DocumentDate,
    string ExtractionStatus,
    string ReviewStatus,
    string? FailureCode,
    string DocumentPath);
