namespace Hakeem.Application.Features.MedicalCvs.DTOs;

public sealed record FocusedMedicalEvidenceResponse(
    string GlobalErrorCode,
    IReadOnlyList<FocusedMedicalEvidence> Data);

public sealed record FocusedMedicalEvidence(
    Guid PointId,
    double Score,
    string Content,
    string FieldName,
    string Value);
