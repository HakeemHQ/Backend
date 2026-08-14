using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.Doctor.MedicalCvs.DTOs;
using Hakeem.Application.Repositories.DoctorPatientAccesses;
using Hakeem.Application.Repositories.DoctorProfiles;
using Hakeem.Application.Repositories.MedicalCvs;
using Hakeem.Domain.Enums.Access;
using MediatR;

namespace Hakeem.Application.Features.Doctor.MedicalCvs.Queries
{
    public sealed class GetPatientMedicalCvsQueryHandler(
        ICurrentUserContext currentUserContext,
        IDoctorProfileRepository doctorProfileRepository,
        IDoctorPatientAccessRepository doctorPatientAccessRepository,
        IMedicalCvRepository medicalCvRepository)
        : IRequestHandler<
            GetPatientMedicalCvsQuery, DoctorMedicalCvsResponse>
    {
        public async Task<DoctorMedicalCvsResponse> Handle(GetPatientMedicalCvsQuery request, CancellationToken cancellationToken)
        {
            if (request.PatientId == Guid.Empty)
            {
                throw new ArgumentException("PatientId is required.");
            }

            if (request.Page < 1)
            {
                throw new ArgumentException("Page must be greater than or equal to 1.");
            }

            if (request.PageSize < 1 || request.PageSize > 100)
            {
                throw new ArgumentException(
                    "PageSize must be between 1 and 100.");
            }

            // 1. Get current doctor
            var doctor = await doctorProfileRepository.GetByUserIdAsync(
                currentUserContext.UserId,
                cancellationToken);

            if (doctor is null)
            {
                throw new UnAuthorizedException(
                    ErrorCodes.AuthUnauthorized);
            }

            // 2. Check active access to this patient
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

            // 3. Get patient's medical CVs
            var medicalCvs = await medicalCvRepository.GetByPatientIdAsync(
                request.PatientId,
                cancellationToken);

            // 4. Pagination
            var items = medicalCvs
                .OrderByDescending(cv => cv.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(cv =>
                {
                    var latestVersion = cv.Versions
                        .OrderByDescending(v => v.VersionNumber)
                        .FirstOrDefault();

                    return new DoctorMedicalCvListItem(
                        cv.Id,
                        cv.Title,
                        latestVersion?.VersionNumber ?? 0,
                        "Patient",
                        latestVersion?.Status.ToString() ?? "Unreviewed"
                    );
                })
                .ToList();

            return new DoctorMedicalCvsResponse(items);
        }
    }
}
