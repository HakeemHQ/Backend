using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.Doctor.MedicalCvs.DTOs;
using Hakeem.Application.Features.MedicalCvs.DTOs;
using Hakeem.Application.Interfaces.MedicalCvs;
using Hakeem.Application.Repositories.DoctorPatientAccesses;
using Hakeem.Application.Repositories.DoctorProfiles;
using Hakeem.Domain.Enums.Identity;
using MediatR;
using System.Globalization;

namespace Hakeem.Application.Features.Doctor.MedicalCvs.Commands
{
    public sealed class GenerateDoctorMedicalCvCommandHandler(
        ICurrentUserContext currentUserContext,
        IDoctorProfileRepository doctorProfileRepository,
        IDoctorPatientAccessRepository doctorPatientAccessRepository,
        IMedicalCvGenerationService generationService)
        : IRequestHandler<
            GenerateDoctorMedicalCvCommand,
            GenerateDoctorMedicalCvResponse>
    {
        public async Task<GenerateDoctorMedicalCvResponse> Handle(
            GenerateDoctorMedicalCvCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Get current Doctor
            var doctor = await doctorProfileRepository.GetByUserIdAsync(
                currentUserContext.UserId,
                cancellationToken);

            if (doctor is null)
            {
                throw new UnAuthorizedException(
                    ErrorCodes.AuthUnauthorized);
            }

            // 2. Doctor must be active
            if (doctor.User.Status != AccountStatus.Active)
            {
                throw new ForbiddenException(
                    "Doctor account is suspended.");
            }

            // 3. Doctor must have active access to this patient
            var hasAccess =
                await doctorPatientAccessRepository.HasActiveAccessAsync(
                    doctor.Id,
                    request.PatientId,
                    DateTime.UtcNow,
                    cancellationToken);

            if (!hasAccess)
            {
                throw new ForbiddenException(
                    "Doctor does not have active access to this patient.");
            }

            // 4. Generate Full CV
            var language = MedicalCvLanguages.Normalize(
                CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);

            var result = await generationService.GenerateFullAsync(
                request.PatientId,
                request.Title,
                language,
                cancellationToken);

            return new GenerateDoctorMedicalCvResponse(
                result.MedicalCvId,
                result.MedicalCvVersionId,
                result.Title,
                result.ScopeType,
                result.Focus,
                result.VersionNumber,
                result.Status.ToString(),
                result.CreatedAt);
        }
    }
}