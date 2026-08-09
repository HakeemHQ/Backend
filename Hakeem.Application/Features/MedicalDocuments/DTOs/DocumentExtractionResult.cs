using System.Text.Json.Serialization;

namespace Hakeem.Application.Features.MedicalDocuments.DTOs;

public sealed record DocumentExtractionResult(
    [property: JsonPropertyName("documentType")]
    MedicalDocumentType DocumentType,

    [property: JsonPropertyName("items")]
    IReadOnlyList<ExtractedItemResult> Items);

public sealed record ExtractedItemResult(
    [property: JsonPropertyName("itemType")]
    string ItemType,

    [property: JsonPropertyName("sequenceNumber")]
    int SequenceNumber,

    [property: JsonPropertyName("pageNumber")]
    int PageNumber,

    [property: JsonPropertyName("fields")]
    IReadOnlyList<ExtractedFieldResult> Fields);

public sealed record ExtractedFieldResult(
    [property: JsonPropertyName("fieldName")]
    string FieldName,

    [property: JsonPropertyName("value")]
    string? Value,

    [property: JsonPropertyName("confidence")]
    decimal? Confidence,

    [property: JsonPropertyName("evidenceText")]
    string? EvidenceText,

    [property: JsonPropertyName("issues")]
    IReadOnlyList<string> Issues);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MedicalDocumentType
{
    Prescription,
    LabReport,
    DischargeSummary,
    MedicalVisit,
    RadiologyReport,
    ClinicalNote,
    Other
}
