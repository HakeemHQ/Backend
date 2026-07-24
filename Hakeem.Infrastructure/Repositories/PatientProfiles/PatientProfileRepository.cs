using Hakeem.Application.DTOs.PatientProfiles;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces.ServiceLifetime;
using Hakeem.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Hakeem.Infrastructure.Repositories.PatientProfiles;

public class PatientProfileRepository : IPatientProfileRepository, IScoped
{
    private readonly ApplicationDbContext _dbContext;

    public PatientProfileRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PatientProfile?> GetByUserIdAsync(
     Guid userId,
     CancellationToken cancellationToken)
    {
        return await _dbContext.PatientProfiles
            .AsNoTracking()
            .Include(x => x.User)
            .SingleOrDefaultAsync(
                x => x.UserId == userId,
                cancellationToken);
    }

    public async Task<PatientProfile?> UpdateByUserIdAsync(
        Guid userId,
        UpdatePatientProfileRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await _dbContext.PatientProfiles
            .Include(x => x.User)
            .SingleOrDefaultAsync(
                x => x.UserId == userId,
                cancellationToken);

        if (profile is null)
        {
            return null;
        }

        if (request.FullName is not null)
        {
            profile.FullName = request.FullName.Trim();
        }

        if (request.BirthDate.HasValue)
        {
            profile.BirthDate = request.BirthDate.Value;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return profile;
    }
}
