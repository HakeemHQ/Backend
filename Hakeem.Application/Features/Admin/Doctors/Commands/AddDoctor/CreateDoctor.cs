using Hakeem.Application.Features.Admin.Doctors.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Features.Admin.Doctors.Commands.AddDoctor
{
    public sealed record CreateDoctorCommand(
    string FirstName,
    string LastName,
    string Email,
    string Specialty,
    string LicenseNumber,
     string Password
) : IRequest<CreateDoctorResponse>;
}
