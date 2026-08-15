using Hakeem.Application.Features.Doctor.MedicalCvs.DTOs;
using Hakeem.Application.Features.MedicalCvs.DTOs;
using Hakeem.Application.Interfaces.Files;
using Hakeem.Application.Interfaces.MedicalCvs;
using Hakeem.Domain.Enums.MedicalCvs;
using MediatR;
using System.Globalization;

namespace Hakeem.Application.Features.Doctor.MedicalCvs.Commands
{
    public sealed class GenerateDoctorMedicalCvCommandHandler(
        IMedicalCvGenerationService generationService,
        IMedicalCvPreviewLinkService previewLinkService,
        IFileUrlResolver fileUrlResolver)
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
            var previewLink = previewLinkService.Create(
                request.PatientId,
                result.MedicalCvId,
                result.MedicalCvVersionId);
            var previewPath =
                $"medical-cv-versions/{result.MedicalCvVersionId}/preview?token=" +
                Uri.EscapeDataString(previewLink.Token);

            return new GenerateDoctorMedicalCvResponse(
                result.MedicalCvId,
                result.Title,
                new GenerateDoctorMedicalCvVersionResponse(
                    result.MedicalCvVersionId,
                    result.VersionNumber,
                    result.Status.ToString(),
                    "Unreviewed",
                    result.CreatedByRole.ToString(),
                    fileUrlResolver.ToAbsoluteUrl(previewPath),
                    previewLink.ExpiresAt));
        }
    }
}
