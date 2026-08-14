using Hakeem.Domain.Enums.MedicalCvs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Features.Doctor.MedicalCvs.DTOs
{
    public sealed record GenerateDoctorMedicalCvResponse(Guid MedicalCvId, GenerateDoctorMedicalCvVersionResponse LatestVersion); 
    public sealed record GenerateDoctorMedicalCvVersionResponse(
        Guid MedicalCvVersionId,
        int VersionNumber, 
        string GenerationStatus,
        string VerificationStatus,
        string CreatedByRole);
}
