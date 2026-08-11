using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Access;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Repositories.DoctorPatientAccesses;

public interface IDoctorPatientAccessRepository : IScoped
{
    Task<IReadOnlyList<DoctorPatientAccess>> GetForDoctorAsync(
        Guid doctorProfileId,
        DoctorPatientAccessStatus status,
        DateTime utcNow,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DoctorPatientAccess>> GetActiveForPatientAsync(
        Guid patientProfileId,
        DateTime utcNow,
        CancellationToken cancellationToken);

    Task<int> RevokeActiveForPatientAsync(
        Guid accessId,
        Guid patientProfileId,
        DateTime revokedAt,
        CancellationToken cancellationToken);
}
