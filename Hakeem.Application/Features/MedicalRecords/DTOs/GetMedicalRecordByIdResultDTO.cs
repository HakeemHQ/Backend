using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Features.MedicalRecords.DTOs
{
    public sealed record GetMedicalRecordByIdResultDTO(
       Guid MedicalRecordId,
       string RecordType,
       string DisplayName,
       DateTime ClinicalDate,
       string Status,
       List<SourceDto> Sources,
       List<MedicalRecordFieldDTO> Fields);
}
