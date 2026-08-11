using Hakeem.Application.Common;
using MediatR;

namespace Hakeem.Application.Features.DoctorPatientAccesses.Queries.GetPatientDoctorAccesses;

public sealed record GetPatientDoctorAccessesQuery(
    int PageNumber = 1,
    int PageSize = 20)
    : IRequest<PaginatedResult<PatientDoctorAccessItem>>;

public sealed record PatientDoctorAccessItem(
    Guid AccessId,
    Guid DoctorId,
    string DoctorName,
    string Specialty,
    DateTime ExpiresAt);
