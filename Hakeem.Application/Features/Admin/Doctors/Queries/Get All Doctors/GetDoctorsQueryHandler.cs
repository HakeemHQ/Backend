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
    : IRequestHandler<GetDoctorsQuery, IReadOnlyList<AdminDoctorListItem>>
    {
        public async Task<IReadOnlyList<AdminDoctorListItem>> Handle(
    GetDoctorsQuery request,
    CancellationToken cancellationToken)
        {

            var page = request.Page < 1 ? 1 : request.Page; 
            var pageSize = request.PageSize <= 0 ? 10 : request.PageSize;
            var doctors = await doctorProfileRepository.GetFilteredAsync(request.Search, request.Specialty, request.Status, page, pageSize, cancellationToken); 
            var items = doctors.Select(doctor => new AdminDoctorListItem(doctor.Id, $"{doctor.User.FirstName} {doctor.User.LastName}".Trim(), doctor.User.Email, doctor.Specialty, doctor.LicenseNumber, doctor.User.Status)).ToList();
            return items;
        }

    }

  
}
