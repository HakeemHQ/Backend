using Hakeem.Domain.Enums.MedicalCvs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Features.Doctor.MedicalCvs.DTOs
{
    public sealed record GenerateDoctorMedicalCvRequest(string Title);
}
