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
        string? fullName,
        DateTime? birthDate,
        string? firstName,
        string? lastName,
        string? phoneNumber,
        string? gender,
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

        if (fullName is not null)
        {
            profile.FullName = fullName.Trim();
        }

        if (birthDate.HasValue)
        {
            profile.BirthDate = birthDate.Value;
        }

        if (firstName is not null)
        {
            profile.User.FirstName = firstName.Trim();
        }

        if (lastName is not null)
        {
            profile.User.LastName = lastName.Trim();
        }

        if (phoneNumber is not null)
        {
            profile.User.PhoneNumber = phoneNumber.Trim();
        }

        if (gender is not null)
        {
            var trimmedGender = gender.Trim();
            profile.User.Gender = char.ToUpperInvariant(trimmedGender[0])
                + trimmedGender[1..].ToLowerInvariant();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return profile;
    }
}
