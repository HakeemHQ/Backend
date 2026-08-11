using Hakeem.Application.Common;
using Hakeem.Domain.Enums.Access;
using MediatR;

namespace Hakeem.Application.Features.PatientAccessRequests.Queries.GetPatientAccessRequests;

public sealed record GetPatientAccessRequestsQuery(
    PatientAccessRequestStatus? Status = null,
    int PageNumber = 1,
    int PageSize = 20)
    : IRequest<PaginatedResult<PatientAccessRequestItem>>;

public sealed record PatientAccessRequestItem(
    Guid RequestId,
    PatientAccessRequestDoctor Doctor,
    string Status,
    DateTime RequestedAt);

public sealed record PatientAccessRequestDoctor(
    Guid DoctorId,
    string FullName,
    string Specialty);
