using Hakeem.Application.Repositories.PatientIdentities;
using Hakeem.Domain.Entities;
using Hakeem.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Hakeem.Infrastructure.Repositories.PatientIdentities;

public sealed class PatientIdentityRepository(ApplicationDbContext dbContext)
    : IPatientIdentityRepository
{
    public Task<PatientProfile?> GetByPatientCodeForUpdateAsync(
        string patientCode,
        CancellationToken cancellationToken)
    {
        return dbContext.PatientProfiles.SingleOrDefaultAsync(
            patient => patient.PatientCode == patientCode,
            cancellationToken);
    }

    public Task<bool> VerifiedNationalIdBelongsToAnotherPatientAsync(
        string verifiedNationalId,
        Guid patientId,
        CancellationToken cancellationToken)
    {
        return dbContext.PatientProfiles.AnyAsync(
            patient =>
                patient.Id != patientId &&
                patient.VerifiedNationalId == verifiedNationalId,
            cancellationToken);
    }

    public Task<bool> VerifiedNationalIdExistsAsync(
        string verifiedNationalId,
        CancellationToken cancellationToken)
    {
        return dbContext.PatientProfiles.AnyAsync(
            patient => patient.VerifiedNationalId == verifiedNationalId,
            cancellationToken);
    }
}
