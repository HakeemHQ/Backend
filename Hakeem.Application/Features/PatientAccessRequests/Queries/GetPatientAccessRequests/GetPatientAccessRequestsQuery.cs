using Hakeem.Domain.Enums.Access;
using MediatR;

namespace Hakeem.Application.Features.PatientAccessRequests.Queries.GetPatientAccessRequests;

public sealed record GetPatientAccessRequestsQuery(
    PatientAccessRequestStatus? Status = null)
    : IRequest<GetPatientAccessRequestsResult>;

public sealed record GetPatientAccessRequestsResult(
    IReadOnlyList<PatientAccessRequestItem> Items);

public sealed record PatientAccessRequestItem(
    Guid RequestId,
    PatientAccessRequestDoctor Doctor,
    string Status,
    DateTime RequestedAt);

public sealed record PatientAccessRequestDoctor(
    Guid DoctorId,
    string FullName,
    string Specialty);
