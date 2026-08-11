using MediatR;

namespace Hakeem.Application.Features.PatientAccessRequests.Commands.RejectPatientAccessRequest;

public sealed record RejectPatientAccessRequestCommand(Guid RequestId)
    : IRequest<RejectPatientAccessRequestResult>;

public sealed record RejectPatientAccessRequestResult(
    Guid RequestId,
    string Status);
