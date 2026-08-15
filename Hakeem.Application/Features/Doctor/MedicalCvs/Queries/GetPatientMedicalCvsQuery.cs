using Hakeem.Application.Features.Doctor.MedicalCvs.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Features.Doctor.MedicalCvs.Queries
{
    public sealed record GetPatientMedicalCvsQuery(Guid PatientId, int Page = 1, int PageSize = 10) : IRequest<DoctorMedicalCvsResponse>;
}
