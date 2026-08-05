namespace Hakeem.Application.Interfaces.Rag;

public sealed record MedicalRecordFieldVectorDocument(
    Guid MedicalRecordFieldId,
    Guid MedicalRecordId,
    Guid PatientProfileId,
    string RecordType,
    string FieldName,
    string Value,
    DateTime ClinicalDate);
