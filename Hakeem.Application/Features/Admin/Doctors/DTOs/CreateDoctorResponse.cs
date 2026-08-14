using Hakeem.Domain.Enums.Identity;
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
    string FullName,
    string Email,
    string Specialty,
    string LicenseNumber,
    AccountStatus Status
);
}
