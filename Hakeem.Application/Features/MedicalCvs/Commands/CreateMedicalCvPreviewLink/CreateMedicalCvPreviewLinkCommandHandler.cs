using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Interfaces.Files;
using Hakeem.Application.Interfaces.MedicalCvs;
using Hakeem.Application.Repositories.MedicalCvs;
using Hakeem.Application.Repositories.PatientProfiles;
using MediatR;

namespace Hakeem.Application.Features.MedicalCvs.Commands.CreateMedicalCvPreviewLink;

public sealed class CreateMedicalCvPreviewLinkCommandHandler(
    ICurrentUserContext currentUserContext,
    IPatientProfileRepository patientProfileRepository,
    IMedicalCvRepository medicalCvRepository,
    IMedicalCvPreviewLinkService previewLinkService,
    IFileUrlResolver fileUrlResolver)
    : IRequestHandler<
        CreateMedicalCvPreviewLinkCommand,
        CreateMedicalCvPreviewLinkResponse>
{
    public async Task<CreateMedicalCvPreviewLinkResponse> Handle(
        CreateMedicalCvPreviewLinkCommand request,
        CancellationToken cancellationToken)
    {
        var patient = await patientProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (patient is null)
        {
            throw new UnAuthorizedException(ErrorCodes.AuthUnauthorized);
        }

        var version = await medicalCvRepository.GetVersionForPatientAsync(
            request.MedicalCvVersionId,
            patient.Id,
            cancellationToken);

        if (version is null)
        {
            throw new NotFoundException(ErrorCodes.MedicalCvNotFound);
        }

        var previewLink = previewLinkService.Create(
            patient.Id,
            version.MedicalCvId,
            request.MedicalCvVersionId);
        var previewPath =
            $"medical-cv-versions/{request.MedicalCvVersionId}/preview?token=" +
            Uri.EscapeDataString(previewLink.Token);

        return new CreateMedicalCvPreviewLinkResponse(
            fileUrlResolver.ToAbsoluteUrl(previewPath),
            previewLink.ExpiresAt);
    }
}
