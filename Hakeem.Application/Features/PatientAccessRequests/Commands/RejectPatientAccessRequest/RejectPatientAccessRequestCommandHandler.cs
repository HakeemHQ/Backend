using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Configurations;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Repositories.PatientAccessRequests;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Domain.Enums.Access;
using MediatR;
using Microsoft.Extensions.Options;

namespace Hakeem.Application.Features.PatientAccessRequests.Commands.RejectPatientAccessRequest;

public sealed class RejectPatientAccessRequestCommandHandler(
    IPatientProfileRepository patientProfileRepository,
    IPatientAccessRequestRepository accessRequestRepository,
    ICurrentUserContext currentUserContext,
    IOptions<PatientAccessConfiguration> options)
    : IRequestHandler<RejectPatientAccessRequestCommand, RejectPatientAccessRequestResult>
{
    private readonly TimeSpan _pendingRequestLifetime = TimeSpan.FromMinutes(
        options.Value.PendingRequestLifetimeMinutes);

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

        var rejectedAt = DateTime.UtcNow;
        var pendingExpiresBefore = rejectedAt.Subtract(_pendingRequestLifetime);
        await accessRequestRepository.ExpireStaleRequestsAsync(
            patient.Id,
            pendingExpiresBefore,
            rejectedAt,
            cancellationToken);

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
            rejectedAt,
            pendingExpiresBefore,
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
