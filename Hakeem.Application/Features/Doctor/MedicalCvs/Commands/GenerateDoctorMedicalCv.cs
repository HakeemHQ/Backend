using Hakeem.Application.Features.Doctor.MedicalCvs.DTOs;
using Hakeem.Domain.Enums.MedicalCvs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Features.Doctor.MedicalCvs.Commands
{
    public sealed record GenerateDoctorMedicalCvCommand(
    Guid PatientId,
    string Title
) : IRequest<GenerateDoctorMedicalCvResponse>;
}
