using MediatR;

namespace Hakeem.Application.Features.PatientAccessRequests.Commands.CreatePatientAccessRequest;

public sealed class CreatePatientAccessRequestCommand
    : IRequest<CreatePatientAccessRequestResult>
{
    public string PatientCode { get; set; } = string.Empty;
}

public sealed record CreatePatientAccessRequestResult(
    Guid RequestId,
    string Status,
    DateTime RequestedAt);
