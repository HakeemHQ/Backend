using MediatR;
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


    public sealed record DoctorMedicalCvListItem(Guid MedicalCvId, string Title, int LatestVersionNumber, string CreatedByRole, string VerificationStatus);

    public sealed record GetDoctorPatientMedicalCvsQuery(
    Guid PatientId,
    int Page = 1,
    int PageSize = 10
) : IRequest<IReadOnlyList<DoctorMedicalCvListItem>>;


    public sealed record DoctorMedicalCvsResponse(IReadOnlyList<DoctorMedicalCvListItem> Items);
}
