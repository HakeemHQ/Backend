namespace Hakeem.Application.Features.MedicalDataExtraction.DTOs;

public sealed record ExtractedFieldsResponse(
    Guid DocumentId,
    string DocumentType,
    string ExtractionStatus,
    IReadOnlyList<ExtractedItemResponse> Items);

public sealed record ExtractedItemResponse(
    Guid ExtractedItemId,
    string ItemType,
    int SequenceNumber,
    int PageNumber,
    IReadOnlyList<ExtractedFieldResponse> Fields);

public sealed record ExtractedFieldResponse(
    Guid ExtractedFieldId,
    string FieldName,
    string? ExtractedValue,
    decimal? Confidence,
    string? EvidenceText,
    IReadOnlyList<string> Issues);
