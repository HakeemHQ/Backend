using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Identity;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Repositories.DoctorProfiles;

public interface IDoctorProfileRepository : IScoped
{
    Task<DoctorProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    void Add(DoctorProfile doctorProfile);

    Task<bool> LicenseNumberExistsAsync(string licenseNumber,CancellationToken cancellationToken);

    Task<IReadOnlyList<DoctorProfile>> GetAllAsync(
    CancellationToken cancellationToken);

    Task<DoctorProfile?> GetByIdAsync(
    Guid doctorId,
    CancellationToken cancellationToken);

    Task<DoctorProfile?> GetByIdForUpdateAsync(
    Guid doctorId,
    CancellationToken cancellationToken);


    Task<IReadOnlyList<DoctorProfile>> GetFilteredAsync(
    string? search,
    string? specialty,
    AccountStatus? status,
    int page,
    int pageSize,
    CancellationToken cancellationToken);
}
