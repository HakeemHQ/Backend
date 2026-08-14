using Hakeem.Application.Features.Admin.Doctors.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Features.Admin.Doctors.Queries.GetDoctorById
{
    public sealed record GetDoctorByIdQuery(
    Guid DoctorId
) : IRequest<AdminDoctorResponse>;
}
