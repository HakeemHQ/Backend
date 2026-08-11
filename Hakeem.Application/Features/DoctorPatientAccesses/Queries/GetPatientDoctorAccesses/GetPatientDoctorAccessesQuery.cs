using MediatR;

namespace Hakeem.Application.Features.DoctorPatientAccesses.Queries.GetPatientDoctorAccesses;

public sealed record GetPatientDoctorAccessesQuery
    : IRequest<GetPatientDoctorAccessesResult>;

public sealed record GetPatientDoctorAccessesResult(
    IReadOnlyList<PatientDoctorAccessItem> Items);

public sealed record PatientDoctorAccessItem(
    Guid AccessId,
    Guid DoctorId,
    string DoctorName,
    string Specialty,
    DateTime ExpiresAt);
