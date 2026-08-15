using MediatR;

namespace Hakeem.Application.Features.Doctor.MedicalIntelligence.Commands.Chat;

public sealed record DoctorMedicalIntelligenceChatCommand(
    Guid PatientProfileId,
    string Message)
    : IRequest<DoctorMedicalIntelligenceChatResponse>;

public sealed record DoctorMedicalIntelligenceChatResponse(
    string Message,
    IReadOnlyList<DoctorMedicalIntelligenceRagResultResponse> RagResults,
    DoctorGeneratedFocusedMedicalCvResponse? GeneratedCv);

public sealed record DoctorMedicalIntelligenceRagResultResponse(
    Guid MedicalRecordId,
    float Score,
    string? RecordType,
    string? DisplayName,
    string? Status,
    DateTime? ClinicalDate,
    string? Content,
    string? Fields);

public sealed record DoctorGeneratedFocusedMedicalCvResponse(
    Guid MedicalCvId,
    Guid MedicalCvVersionId,
    string Title,
    string Focus,
    int VersionNumber,
    string Status,
    DateTime CreatedAt,
    string PreviewUrl,
    DateTimeOffset PreviewExpiresAt);
