using Hakeem.Application.DTOs.PatientProfiles;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Repositories.PatientProfiles;

public interface IPatientProfileRepository
{
    Task<PatientProfile?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<PatientProfile?> UpdateByUserIdAsync(
        Guid userId,
        UpdatePatientProfileRequest request,
        CancellationToken cancellationToken);
}
