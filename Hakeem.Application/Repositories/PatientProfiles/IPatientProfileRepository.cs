using Hakeem.Domain.Entities;

namespace Hakeem.Application.Repositories.PatientProfiles;

public interface IPatientProfileRepository
{
    Task<PatientProfile?> GetByIdAsync(
        Guid patientProfileId,
        CancellationToken cancellationToken);

    Task<PatientProfile?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<PatientProfile?> UpdateByUserIdAsync(
        Guid userId,
        string? fullName,
        DateTime? birthDate,
        string? firstName,
        string? lastName,
        string? phoneNumber,
        string? gender,
        CancellationToken cancellationToken);
}
