using Hakeem.Application.Repositories.DoctorProfiles;
using Hakeem.Domain.Entities;
using Hakeem.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Hakeem.Infrastructure.Repositories.DoctorProfiles;

public sealed class DoctorProfileRepository(ApplicationDbContext dbContext)
    : IDoctorProfileRepository
{
    public Task<DoctorProfile?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.DoctorProfiles
            .AsNoTracking()
            .Include(profile => profile.User)
            .SingleOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);
    }
}
