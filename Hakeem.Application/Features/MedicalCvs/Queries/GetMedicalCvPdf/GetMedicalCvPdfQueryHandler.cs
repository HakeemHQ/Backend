using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Interfaces.MedicalCvs;
using Hakeem.Application.Repositories.MedicalCvs;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Domain.Enums.MedicalCvs;
using MediatR;

namespace Hakeem.Application.Features.MedicalCvs.Queries.GetMedicalCvPdf;

public sealed class GetMedicalCvPdfQueryHandler(
    ICurrentUserContext currentUserContext,
    IPatientProfileRepository patientProfileRepository,
    IMedicalCvRepository medicalCvRepository,
    IMedicalCvFileStorage fileStorage)
    : IRequestHandler<GetMedicalCvPdfQuery, GetMedicalCvPdfResult>
{
    public async Task<GetMedicalCvPdfResult> Handle(
        GetMedicalCvPdfQuery request,
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
            request.MedicalCvId,
            request.MedicalCvVersionId,
            patient.Id,
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

        var content = await fileStorage.OpenReadAsync(
            version.PdfFileKey,
            cancellationToken);

        return new GetMedicalCvPdfResult(content);
    }
}
