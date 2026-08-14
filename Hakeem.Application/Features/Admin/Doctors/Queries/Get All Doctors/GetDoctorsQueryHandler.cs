using Hakeem.Application.Features.Admin.Doctors.DTOs;
using Hakeem.Application.Repositories.DoctorProfiles;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Features.Admin.Doctors.Queries
{
    public sealed class GetDoctorsQueryHandler(
     IDoctorProfileRepository doctorProfileRepository)
     : IRequestHandler<GetDoctorsQuery, IReadOnlyList<AdminDoctorResponse>>
    {
        public async Task<IReadOnlyList<AdminDoctorResponse>> Handle(
            GetDoctorsQuery request,
            CancellationToken cancellationToken)
        {
            var doctors = await doctorProfileRepository.GetAllAsync(
                cancellationToken);

            return doctors
                .Select(doctor => new AdminDoctorResponse(
                    doctor.Id,
                    $"{doctor.User.FirstName} {doctor.User.LastName}".Trim(),
                    doctor.User.Email,
                    doctor.Specialty,
                    doctor.LicenseNumber,
                    doctor.User.Status))
                .ToList();
        }
    }
}
