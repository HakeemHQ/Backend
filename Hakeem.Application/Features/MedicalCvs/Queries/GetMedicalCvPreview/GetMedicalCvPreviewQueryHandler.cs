using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.MedicalCvs.Queries.GetMedicalCvPdf;
using Hakeem.Application.Interfaces.MedicalCvs;
using Hakeem.Application.Repositories.MedicalCvs;
using Hakeem.Domain.Enums.MedicalCvs;
using MediatR;

namespace Hakeem.Application.Features.MedicalCvs.Queries.GetMedicalCvPreview;

public sealed class GetMedicalCvPreviewQueryHandler(
    IMedicalCvPreviewLinkService previewLinkService,
    IMedicalCvRepository medicalCvRepository,
    IMedicalCvFileStorage fileStorage)
    : IRequestHandler<GetMedicalCvPreviewQuery, GetMedicalCvPdfResult>
{
    public async Task<GetMedicalCvPdfResult> Handle(
        GetMedicalCvPreviewQuery request,
        CancellationToken cancellationToken)
    {
        if (!previewLinkService.TryValidate(
                request.Token,
                request.MedicalCvId,
                request.MedicalCvVersionId,
                out var access))
        {
            throw new UnAuthorizedException(
                ErrorCodes.MedicalCvPreviewInvalidOrExpired);
        }

        var version = await medicalCvRepository.GetVersionForPatientAsync(
            request.MedicalCvId,
            request.MedicalCvVersionId,
            access.PatientId,
            cancellationToken);

        if (version is null)
        {
            throw new NotFoundException(ErrorCodes.MedicalCvNotFound);
        }

        if (version.Status is MedicalCvVersionStatus.Queued or MedicalCvVersionStatus.Processing)
        {
            throw new ConflictException(ErrorCodes.MedicalCvNotReady);
        }

        if (version.Status == MedicalCvVersionStatus.Failed)
        {
            throw new ServiceUnavailableException(
                ErrorCodes.MedicalCvGenerationFailed);
        }

        if (string.IsNullOrWhiteSpace(version.PdfFileKey))
        {
            throw new ConflictException(ErrorCodes.MedicalCvNotReady);
        }

        return new GetMedicalCvPdfResult(
            await fileStorage.OpenReadAsync(
                version.PdfFileKey,
                cancellationToken));
    }
}
