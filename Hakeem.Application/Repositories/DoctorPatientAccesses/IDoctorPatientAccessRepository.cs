using Hakeem.Application.Common;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Access;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Repositories.DoctorPatientAccesses;

public interface IDoctorPatientAccessRepository : IScoped
{
    Task<PaginatedResult<DoctorPatientAccess>> GetForDoctorAsync(
        Guid doctorProfileId,
        DoctorPatientAccessStatus status,
        DateTime utcNow,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PaginatedResult<DoctorPatientAccess>> GetActiveForPatientAsync(
        Guid patientProfileId,
        DateTime utcNow,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<int> RevokeActiveForPatientAsync(
        Guid accessId,
        Guid patientProfileId,
        DateTime revokedAt,
        CancellationToken cancellationToken);

    Task<int> RevokeAllActiveForDoctorAsync(
        Guid doctorProfileId,
        DateTime revokedAt,
        CancellationToken cancellationToken);

    Task<bool> HasActiveAccessAsync(
    Guid doctorProfileId,
    Guid patientProfileId,
    DateTime utcNow,
    CancellationToken cancellationToken);

}
