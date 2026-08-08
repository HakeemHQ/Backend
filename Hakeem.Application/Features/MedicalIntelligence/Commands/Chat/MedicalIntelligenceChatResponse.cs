namespace Hakeem.Application.Features.MedicalIntelligence.Commands.Chat;

public sealed record MedicalIntelligenceChatResponse(
    string Message,
    IReadOnlyList<MedicalIntelligenceRagResultResponse> RagResults,
    GeneratedFocusedMedicalCvResponse? GeneratedCv);

public sealed record MedicalIntelligenceRagResultResponse(
    Guid MedicalRecordId,
    float Score,
    string? RecordType,
    string? DisplayName,
    string? Status,
    DateTime? ClinicalDate,
    string? Content,
    string? Fields);

public sealed record GeneratedFocusedMedicalCvResponse(
    Guid MedicalCvId,
    Guid MedicalCvVersionId,
    string Title,
    string Focus,
    int VersionNumber,
    string Status,
    DateTime CreatedAt,
    string PreviewUrl,
    DateTimeOffset PreviewExpiresAt);
