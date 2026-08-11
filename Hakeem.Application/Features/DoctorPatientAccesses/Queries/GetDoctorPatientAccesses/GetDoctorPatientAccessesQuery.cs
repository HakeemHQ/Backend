using Hakeem.Domain.Enums.Access;
using MediatR;

namespace Hakeem.Application.Features.DoctorPatientAccesses.Queries.GetDoctorPatientAccesses;

public sealed record GetDoctorPatientAccessesQuery(
    DoctorPatientAccessStatus Status = DoctorPatientAccessStatus.Active)
    : IRequest<GetDoctorPatientAccessesResult>;

public sealed record GetDoctorPatientAccessesResult(
    IReadOnlyList<DoctorPatientAccessItem> Items);

public sealed record DoctorPatientAccessItem(
    Guid AccessId,
    Guid PatientId,
    string PatientCode,
    string FullName,
    DateTime ExpiresAt);
