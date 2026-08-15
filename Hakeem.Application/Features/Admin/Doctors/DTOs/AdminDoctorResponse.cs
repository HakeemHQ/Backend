using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Features.Admin.Doctors.DTOs
{
    public sealed record AdminDoctorResponse(
    Guid DoctorId,
    Guid UserId,
    string FullName,
    string Email,
    string Specialty,
    string LicenseNumber,
    string Status,
    DateTimeOffset CreatedAt
);
}
