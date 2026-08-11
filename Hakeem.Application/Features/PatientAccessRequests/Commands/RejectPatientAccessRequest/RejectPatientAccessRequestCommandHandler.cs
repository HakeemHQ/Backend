using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Repositories.PatientAccessRequests;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Domain.Enums.Access;
using MediatR;

namespace Hakeem.Application.Features.PatientAccessRequests.Commands.RejectPatientAccessRequest;

public sealed class RejectPatientAccessRequestCommandHandler(
    IPatientProfileRepository patientProfileRepository,
    IPatientAccessRequestRepository accessRequestRepository,
    ICurrentUserContext currentUserContext)
    : IRequestHandler<RejectPatientAccessRequestCommand, RejectPatientAccessRequestResult>
{
    public async Task<RejectPatientAccessRequestResult> Handle(
        RejectPatientAccessRequestCommand request,
        CancellationToken cancellationToken)
    {
        var patient = await patientProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (patient is null)
        {
            throw new UnAuthorizedException(ErrorCodes.AuthUnauthorized);
        }

        var accessRequest = await accessRequestRepository.GetByIdForPatientAsync(
            request.RequestId,
            patient.Id,
            cancellationToken);

        if (accessRequest is null)
        {
            throw new NotFoundException(ErrorCodes.PatientAccessRequestNotFound);
        }

        if (accessRequest.Status != PatientAccessRequestStatus.Pending)
        {
            throw new ConflictException(ErrorCodes.PatientAccessRequestAlreadyActedOn);
        }

        var affectedRows = await accessRequestRepository.RejectPendingAsync(
            accessRequest.Id,
            patient.Id,
            DateTime.UtcNow,
            cancellationToken);

        if (affectedRows != 1)
        {
            throw new ConflictException(ErrorCodes.PatientAccessRequestAlreadyActedOn);
        }

        return new RejectPatientAccessRequestResult(
            accessRequest.Id,
            PatientAccessRequestStatus.Rejected.ToString());
    }
}
