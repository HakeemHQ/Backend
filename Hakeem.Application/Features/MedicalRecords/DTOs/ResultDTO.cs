namespace Hakeem.Application.Features.MedicalRecords.DTOs
{
    public sealed record GetMedicalRecordByIdResult(Guid MedicalRecordId,string RecordType,string DisplayName,
                                                    DateTime ClinicalDate,string Status,
                                                    List<SourceDto> Sources, List<MedicalRecordFieldDTO> Fields);
}
