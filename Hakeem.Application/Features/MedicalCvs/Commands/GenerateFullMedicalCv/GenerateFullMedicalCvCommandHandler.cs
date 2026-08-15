using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.MedicalCvs.DTOs;
using Hakeem.Application.Interfaces.Files;
using Hakeem.Application.Interfaces.MedicalCvs;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Domain.Enums.MedicalCvs;
using MediatR;
using System.Globalization;

namespace Hakeem.Application.Features.MedicalCvs.Commands.GenerateFullMedicalCv;

public sealed class GenerateFullMedicalCvCommandHandler(
    ICurrentUserContext currentUserContext,
    IPatientProfileRepository patientProfileRepository,
    IMedicalCvGenerationService generationService,
    IMedicalCvPreviewLinkService previewLinkService,
    IFileUrlResolver fileUrlResolver)
    : IRequestHandler<
        GenerateFullMedicalCvCommand,
        GenerateFullMedicalCvResponse>
{
    public async Task<GenerateFullMedicalCvResponse> Handle(
        GenerateFullMedicalCvCommand request,
        CancellationToken cancellationToken)
    {
        var patient = await patientProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (patient is null)
        {
            throw new UnAuthorizedException(ErrorCodes.AuthUnauthorized);
        }

        var result = await generationService.GenerateFullAsync(
            patient.Id,
            request.Title,
            MedicalCvLanguages.Normalize(
                CultureInfo.CurrentUICulture.TwoLetterISOLanguageName),
            MedicalCvCreatedByRole.Patient,
            cancellationToken);
        var previewLink = previewLinkService.Create(
            patient.Id,
            result.MedicalCvId,
            result.MedicalCvVersionId);
        var previewPath =
            $"medical-cv-versions/{result.MedicalCvVersionId}/preview?token=" +
            Uri.EscapeDataString(previewLink.Token);

        return new GenerateFullMedicalCvResponse(
            result.MedicalCvId,
            result.Title,
            new LatestMedicalCvVersionResponse(
                result.MedicalCvVersionId,
                result.VersionNumber,
                result.Status.ToString(),
                "Unreviewed",
                result.CreatedByRole.ToString(),
                fileUrlResolver.ToAbsoluteUrl(previewPath),
                previewLink.ExpiresAt));
    }
}
