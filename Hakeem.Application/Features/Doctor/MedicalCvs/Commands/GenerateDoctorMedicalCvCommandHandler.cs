using Hakeem.Application.Features.Doctor.MedicalCvs.DTOs;
using Hakeem.Application.Features.MedicalCvs.DTOs;
using Hakeem.Application.Interfaces.MedicalCvs;
using Hakeem.Domain.Enums.MedicalCvs;
using MediatR;
using System.Globalization;

namespace Hakeem.Application.Features.Doctor.MedicalCvs.Commands
{
    public sealed class GenerateDoctorMedicalCvCommandHandler(
        IMedicalCvGenerationService generationService)
        : IRequestHandler<
            GenerateDoctorMedicalCvCommand,
            GenerateDoctorMedicalCvResponse>
    {
        public async Task<GenerateDoctorMedicalCvResponse> Handle(
            GenerateDoctorMedicalCvCommand request,
            CancellationToken cancellationToken)
        {
            var language = MedicalCvLanguages.Normalize(
                CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);

            var result = await generationService.GenerateFullAsync(
                request.PatientId,
                request.Title,
                language,
                MedicalCvCreatedByRole.Doctor,
                cancellationToken);

            return new GenerateDoctorMedicalCvResponse(
                result.MedicalCvId,
                result.Title,
                new GenerateDoctorMedicalCvVersionResponse(
                    result.MedicalCvVersionId,
                    result.VersionNumber,
                    result.Status.ToString(),
                    "Unreviewed",
                    result.CreatedByRole.ToString()));
        }
    }
}
