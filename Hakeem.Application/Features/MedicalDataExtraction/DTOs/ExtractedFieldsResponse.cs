namespace Hakeem.Application.Features.MedicalDataExtraction.DTOs;

public sealed record ExtractedFieldsResponse(
    Guid DocumentId,
    string DocumentType,
    string ExtractionStatus,
    string ReviewStatus,
    IReadOnlyList<ExtractedItemResponse> Items);

public sealed record ExtractedItemResponse(
    Guid ExtractedItemId,
    string ItemType,
    int SequenceNumber,
    int PageNumber,
    string ReviewStatus,
    IReadOnlyList<ExtractedFieldResponse> Fields);

public sealed record ExtractedFieldResponse(
    Guid ExtractedFieldId,
    string FieldName,
    string? ExtractedValue,
    decimal? Confidence,
    string? EvidenceText,
    IReadOnlyList<string> Issues);
