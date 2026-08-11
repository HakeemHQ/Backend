using Hakeem.Application.Common;
using Hakeem.Domain.Enums.Access;
using MediatR;

namespace Hakeem.Application.Features.DoctorPatientAccesses.Queries.GetDoctorPatientAccesses;

public sealed record GetDoctorPatientAccessesQuery(
    DoctorPatientAccessStatus Status = DoctorPatientAccessStatus.Active,
    int PageNumber = 1,
    int PageSize = 20)
    : IRequest<PaginatedResult<DoctorPatientAccessItem>>;

public sealed record DoctorPatientAccessItem(
    Guid AccessId,
    Guid PatientId,
    string PatientCode,
    string FullName,
    DateTime ExpiresAt);
