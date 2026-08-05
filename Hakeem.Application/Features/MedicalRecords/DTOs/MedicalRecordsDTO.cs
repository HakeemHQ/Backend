namespace Hakeem.Application.Features.MedicalRecords.DTOs
{
public sealed class MedicalRecordDto
{
    public Guid MedicalRecordId { get; init; }

    public string RecordType { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public DateTime ClinicalDate { get; init; }

    public string Status { get; init; } = string.Empty;
}
}
