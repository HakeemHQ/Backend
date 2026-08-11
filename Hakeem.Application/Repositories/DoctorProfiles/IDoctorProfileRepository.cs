using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Repositories.DoctorProfiles;

public interface IDoctorProfileRepository : IScoped
{
    Task<DoctorProfile?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken);
}
