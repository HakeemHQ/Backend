using System.Text.Json.Serialization;

namespace Hakeem.Application.Features.MedicalDocuments.DTOs;

public sealed record MedicalDocumentClassification(
    [property: JsonPropertyName("isMedical")]
    bool IsMedical,

    [property: JsonPropertyName("documentType")]
    MedicalDocumentType? DocumentType,

    [property: JsonPropertyName("confidence")]
    double Confidence,

    [property: JsonPropertyName("rejectionReason")]
    string? RejectionReason);
