using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Repositories.PatientIdentities;

public interface IPatientIdentityRepository : IScoped
{
    Task<PatientProfile?> GetByPatientCodeForUpdateAsync(
        string patientCode,
        CancellationToken cancellationToken);

    Task<bool> VerifiedNationalIdBelongsToAnotherPatientAsync(
        string verifiedNationalId,
        Guid patientId,
        CancellationToken cancellationToken);
}
