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
    public void Add(DoctorProfile doctorProfile)
    {
        dbContext.DoctorProfiles.Add(doctorProfile);
    }

    public Task<bool> LicenseNumberExistsAsync(
        string licenseNumber,
        CancellationToken cancellationToken)
    {
        return dbContext.DoctorProfiles
            .AnyAsync(
                profile => profile.LicenseNumber == licenseNumber,
                cancellationToken);
    }


    public async Task<IReadOnlyList<DoctorProfile>> GetAllAsync(
    CancellationToken cancellationToken)
    {
        return await dbContext.DoctorProfiles
            .AsNoTracking()
            .Include(profile => profile.User)
            .ToListAsync(cancellationToken);
    }

    public Task<DoctorProfile?> GetByIdAsync(
    Guid doctorId,
    CancellationToken cancellationToken)
    {
        return dbContext.DoctorProfiles
            .AsNoTracking()
            .Include(profile => profile.User)
            .SingleOrDefaultAsync(
                profile => profile.Id == doctorId,
                cancellationToken);
    }


    public Task<DoctorProfile?> GetByIdForUpdateAsync(
    Guid doctorId,
    CancellationToken cancellationToken)
    {
        return dbContext.DoctorProfiles
            .Include(profile => profile.User)
            .SingleOrDefaultAsync(
                profile => profile.Id == doctorId,
                cancellationToken);
    }
}
