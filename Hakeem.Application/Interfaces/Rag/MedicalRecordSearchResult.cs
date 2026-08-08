namespace Hakeem.Application.Interfaces.Rag;

public sealed record MedicalRecordSearchResult(
    Guid MedicalRecordId,
    Guid PatientProfileId,
    float Score,
    string? RecordType,
    string? DisplayName,
    string? Status,
    DateTime? ClinicalDate,
    string? Content,
    string? Fields);
