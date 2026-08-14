using Hakeem.Domain.Enums.MedicalCvs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Features.Doctor.MedicalCvs.DTOs
{
    public sealed record GenerateDoctorMedicalCvResponse(
    Guid MedicalCvId,
    Guid MedicalCvVersionId,
    string Title,
    MedicalCvScopeType ScopeType,
    string? Focus,
    int VersionNumber,
    string Status,
    DateTime CreatedAt);
}
