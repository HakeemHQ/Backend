using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Features.Doctor.MedicalCvs.DTOs
{
    public sealed record DoctorMedicalCvResponse(
     Guid MedicalCvId,
     string Title,
     string ScopeType,
     string? Focus,
     IReadOnlyList<DoctorMedicalCvVersionResponse> Versions);

    public sealed record DoctorMedicalCvVersionResponse(
        Guid MedicalCvVersionId,
        int VersionNumber,
        string Status,
        DateTime CreatedAt,
        string? PdfFileKey);
}
