using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Configurations;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Interfaces.Access;
using Hakeem.Application.Repositories.PatientAccessRequests;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Domain.Enums.Access;
using MediatR;
using Microsoft.Extensions.Options;

namespace Hakeem.Application.Features.PatientAccessRequests.Commands.ApprovePatientAccessRequest;

public sealed class ApprovePatientAccessRequestCommandHandler(
    IPatientProfileRepository patientProfileRepository,
    IPatientAccessRequestRepository accessRequestRepository,
    IOneTimeAccessCodeService accessCodeService,
    ICurrentUserContext currentUserContext,
    IOptions<PatientAccessConfiguration> options)
    : IRequestHandler<ApprovePatientAccessRequestCommand, ApprovePatientAccessRequestResult>
{
    private readonly TimeSpan _codeLifetime = TimeSpan.FromMinutes(
        options.Value.CodeLifetimeMinutes);
    private readonly TimeSpan _pendingRequestLifetime = TimeSpan.FromMinutes(
        options.Value.PendingRequestLifetimeMinutes);

    public async Task<ApprovePatientAccessRequestResult> Handle(
        ApprovePatientAccessRequestCommand request,
        CancellationToken cancellationToken)
    {
        var patient = await patientProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (patient is null)
        {
            throw new UnAuthorizedException(ErrorCodes.AuthUnauthorized);
        }

        var approvedAt = DateTime.UtcNow;
        var pendingExpiresBefore = approvedAt.Subtract(_pendingRequestLifetime);
        await accessRequestRepository.ExpireStaleRequestsAsync(
            patient.Id,
            pendingExpiresBefore,
            approvedAt,
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

        var issuedCode = accessCodeService.Generate();
        var codeExpiresAt = approvedAt.Add(_codeLifetime);
        var affectedRows = await accessRequestRepository.ApprovePendingAsync(
            accessRequest.Id,
            patient.Id,
            issuedCode.CodeHash,
            approvedAt,
            codeExpiresAt,
            pendingExpiresBefore,
            cancellationToken);

        if (affectedRows != 1)
        {
            throw new ConflictException(ErrorCodes.PatientAccessRequestAlreadyActedOn);
        }

        return new ApprovePatientAccessRequestResult(
            accessRequest.Id,
            PatientAccessRequestStatus.Approved.ToString(),
            issuedCode.Code,
            codeExpiresAt);
    }
}
