using MediatR;

namespace Hakeem.Application.Features.PatientAccessRequests.Commands.ApprovePatientAccessRequest;

public sealed record ApprovePatientAccessRequestCommand(Guid RequestId)
    : IRequest<ApprovePatientAccessRequestResult>;

public sealed record ApprovePatientAccessRequestResult(
    Guid RequestId,
    string Status,
    string OneTimeCode,
    DateTime CodeExpiresAt);
