namespace Hakeem.Application.Interfaces.Rag;

public sealed record MedicalRecordVectorDocument(
    Guid MedicalRecordId,
    Guid PatientProfileId,
    string RecordType,
    string DisplayName,
    string Status,
    DateTime ClinicalDate,
    IReadOnlyList<MedicalRecordFieldPayload> Fields);

public sealed record MedicalRecordFieldPayload(
    string FieldName,
    string Value);
