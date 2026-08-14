using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.Doctor.MedicalCvs.DTOs;
using Hakeem.Application.Repositories.DoctorPatientAccesses;
using Hakeem.Application.Repositories.DoctorProfiles;
using Hakeem.Application.Repositories.MedicalCvs;
using Hakeem.Domain.Enums.Access;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Features.Doctor.MedicalCvs.Queries
{
    public sealed class GetPatientMedicalCvsQueryHandler(
    ICurrentUserContext currentUserContext,
    IDoctorProfileRepository doctorProfileRepository,
    IDoctorPatientAccessRepository doctorPatientAccessRepository,
    IMedicalCvRepository medicalCvRepository)
    : IRequestHandler<
        GetPatientMedicalCvsQuery,
        IReadOnlyList<DoctorMedicalCvResponse>>
    {
        public async Task<IReadOnlyList<DoctorMedicalCvResponse>> Handle(
            GetPatientMedicalCvsQuery request,
            CancellationToken cancellationToken)
        {
            if (request.PatientId == Guid.Empty)
            {
                throw new ArgumentException("PatientId is required.");
            }

            var doctor = await doctorProfileRepository.GetByUserIdAsync(
                currentUserContext.UserId,
                cancellationToken);

            if (doctor is null)
            {
                throw new UnAuthorizedException(
                    ErrorCodes.AuthUnauthorized);
            }

            var accesses = await doctorPatientAccessRepository.GetForDoctorAsync(
                doctor.Id,
                DoctorPatientAccessStatus.Active,
                DateTime.UtcNow,
                1,
                int.MaxValue,
                cancellationToken);

            var hasAccess = accesses.Items.Any(
                x => x.PatientProfileId == request.PatientId);

            if (!hasAccess)
            {
                throw new ForbiddenException(
                    "Doctor does not have active access to this patient.");
            }

            var medicalCvs = await medicalCvRepository.GetByPatientIdAsync(
                request.PatientId,
                cancellationToken);

            return medicalCvs
                .Select(cv => new DoctorMedicalCvResponse(
                    cv.Id,
                    cv.Title,
                    cv.ScopeType.ToString(),
                    cv.Focus,
                    cv.Versions
                        .OrderByDescending(v => v.VersionNumber)
                        .Select(v => new DoctorMedicalCvVersionResponse(
                            v.Id,
                            v.VersionNumber,
                            v.Status.ToString(),
                            v.CreatedAt,
                            v.PdfFileKey))
                        .ToList()))
                .ToList();
        }
    }
}
