using Hakeem.Application.Repositories.DoctorPatientAccesses;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Access;
using Hakeem.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Hakeem.Infrastructure.Repositories.DoctorPatientAccesses;

public sealed class DoctorPatientAccessRepository(ApplicationDbContext dbContext)
    : IDoctorPatientAccessRepository
{
    public async Task<IReadOnlyList<DoctorPatientAccess>> GetForDoctorAsync(
        Guid doctorProfileId,
        DoctorPatientAccessStatus status,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var query = dbContext.DoctorPatientAccesses
            .AsNoTracking()
            .Include(access => access.Patient)
            .Where(access => access.DoctorProfileId == doctorProfileId);

        query = status switch
        {
            DoctorPatientAccessStatus.Active => query.Where(access =>
                access.Status == DoctorPatientAccessStatus.Active &&
                access.ExpiresAt > utcNow),
            DoctorPatientAccessStatus.Expired => query.Where(access =>
                access.Status == DoctorPatientAccessStatus.Expired ||
                (access.Status == DoctorPatientAccessStatus.Active &&
                 access.ExpiresAt <= utcNow)),
            DoctorPatientAccessStatus.Revoked => query.Where(access =>
                access.Status == DoctorPatientAccessStatus.Revoked),
            _ => query.Where(_ => false)
        };

        return await query
            .OrderBy(access => access.ExpiresAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DoctorPatientAccess>> GetActiveForPatientAsync(
        Guid patientProfileId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        return await dbContext.DoctorPatientAccesses
            .AsNoTracking()
            .Include(access => access.Doctor)
                .ThenInclude(doctor => doctor.User)
            .Where(access =>
                access.PatientProfileId == patientProfileId &&
                access.Status == DoctorPatientAccessStatus.Active &&
                access.ExpiresAt > utcNow)
            .OrderBy(access => access.ExpiresAt)
            .ToListAsync(cancellationToken);
    }

    public Task<int> RevokeActiveForPatientAsync(
        Guid accessId,
        Guid patientProfileId,
        DateTime revokedAt,
        CancellationToken cancellationToken)
    {
        return dbContext.DoctorPatientAccesses
            .Where(access =>
                access.Id == accessId &&
                access.PatientProfileId == patientProfileId &&
                access.Status == DoctorPatientAccessStatus.Active &&
                access.ExpiresAt > revokedAt)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(access => access.Status, DoctorPatientAccessStatus.Revoked)
                    .SetProperty(access => access.RevokedAt, revokedAt)
                    .SetProperty(access => access.UpdatedAt, revokedAt),
                cancellationToken);
    }
}
