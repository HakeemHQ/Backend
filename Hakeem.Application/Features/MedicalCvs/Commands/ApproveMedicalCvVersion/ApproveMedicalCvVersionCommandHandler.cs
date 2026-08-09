using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Repositories.MedicalCvs;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Domain.Enums.MedicalCvs;
using Hakeem.Domain.Interfaces;
using MediatR;

namespace Hakeem.Application.Features.MedicalCvs.Commands.ApproveMedicalCvVersion;

public sealed class ApproveMedicalCvVersionCommandHandler(
    ICurrentUserContext currentUserContext,
    IPatientProfileRepository patientProfileRepository,
    IMedicalCvRepository medicalCvRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<
        ApproveMedicalCvVersionCommand,
        ApproveMedicalCvVersionResponse>
{
    public async Task<ApproveMedicalCvVersionResponse> Handle(
        ApproveMedicalCvVersionCommand request,
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

        if (version.Status != MedicalCvVersionStatus.Draft)
        {
            throw new ConflictException(ErrorCodes.MedicalCvVersionNotDraft);
        }

        var approvedAt = DateTime.UtcNow;
        version.Status = MedicalCvVersionStatus.Approved;
        version.ApprovedAt = approvedAt;
        await unitOfWork.SaveChanges(cancellationToken);

        return new ApproveMedicalCvVersionResponse(
            version.Id,
            version.VersionNumber,
            version.Status,
            approvedAt);
    }
}
