using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Features.Admin.Doctors.DTOs
{
    public sealed record CreateDoctorResponse(
    Guid UserId,
    Guid DoctorId,
    string Email,
    string FullName,
    string Specialty,
    string LicenseNumber,
    string Status
);
}
