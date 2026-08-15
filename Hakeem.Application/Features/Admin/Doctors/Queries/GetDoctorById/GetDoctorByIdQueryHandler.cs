using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.Admin.Doctors.DTOs;
using Hakeem.Application.Repositories.DoctorProfiles;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Features.Admin.Doctors.Queries.GetDoctorById
{
    public sealed class GetDoctorByIdQueryHandler(
    IDoctorProfileRepository doctorProfileRepository)
    : IRequestHandler<GetDoctorByIdQuery, AdminDoctorResponse>
    {
        public async Task<AdminDoctorResponse> Handle(
            GetDoctorByIdQuery request,
            CancellationToken cancellationToken)
        {
            var doctor = await doctorProfileRepository.GetByIdAsync(
                request.DoctorId,
                cancellationToken);

            if (doctor is null)
            {
                throw new NotFoundException(ErrorCodes.DoctorNotFound);
            }

            return new AdminDoctorResponse(
                DoctorId: doctor.Id,
                UserId: doctor.UserId,
                FullName: $"{doctor.User.FirstName} {doctor.User.LastName}".Trim(),
                Email: doctor.User.Email,
                Specialty: doctor.Specialty,
                LicenseNumber: doctor.LicenseNumber,
                Status: doctor.User.Status.ToString(),
                CreatedAt: new DateTimeOffset(
                    DateTime.SpecifyKind(doctor.CreatedAt, DateTimeKind.Utc))
 );
        }
    }
}
