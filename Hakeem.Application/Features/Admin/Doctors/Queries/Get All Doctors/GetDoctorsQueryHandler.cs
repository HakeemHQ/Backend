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
    : IRequestHandler<GetDoctorsQuery, AdminDoctorsResponse>
    {
        public async Task<AdminDoctorsResponse> Handle(
    GetDoctorsQuery request,
    CancellationToken cancellationToken)
        {

            var doctors = await doctorProfileRepository.GetFilteredAsync(
                request.Search,
                request.Specialty,
                request.Status,
                request.Page,
                request.PageSize,
                cancellationToken);
            var items = doctors
                .Select(doctor => new AdminDoctorListItem(
                    doctor.Id,
                    $"{doctor.User.FirstName} {doctor.User.LastName}".Trim(),
                    doctor.User.Email,
                    doctor.Specialty,
                    doctor.LicenseNumber,
                    doctor.User.Status.ToString()))
                .ToList();
            return new AdminDoctorsResponse(items);
        }

    }

  
}
