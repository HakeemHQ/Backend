using Hakeem.Domain.Enums.MedicalCvs;

namespace Hakeem.Application.Interfaces.Agents;

public interface IMedicalIntelligenceAgent
{
    Task<MedicalIntelligenceResponse> RespondAsync(
        Guid patientId,
        string message,
        CancellationToken cancellationToken = default);
}

public sealed record MedicalIntelligenceResponse(
    string Message,
    MedicalIntelligenceCapability Capability,
    IReadOnlyList<PatientMedicalEvidenceItem> RagResults,
    FocusedMedicalCvActionResult? FocusedMedicalCv = null);

public enum MedicalIntelligenceCapability
{
    None,
    PatientEvidence,
    FocusedMedicalCv
}

public sealed record FocusedMedicalCvActionResult(
    Guid MedicalCvId,
    Guid MedicalCvVersionId,
    string Title,
    string Focus,
    int VersionNumber,
    MedicalCvVersionStatus Status,
    DateTime CreatedAt);

public sealed record PatientMedicalEvidenceItem(
    Guid MedicalRecordId,
    float Score,
    string? RecordType,
    string? DisplayName,
    string? Status,
    DateTime? ClinicalDate,
    string? Content,
    string? Fields);
