using Hakeem.Application.Common;
using Hakeem.Application.Repositories.DoctorPatientAccesses;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Access;
using Hakeem.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Hakeem.Infrastructure.Repositories.DoctorPatientAccesses;

public sealed class DoctorPatientAccessRepository(ApplicationDbContext dbContext)
    : IDoctorPatientAccessRepository
{
    public async Task<PaginatedResult<DoctorPatientAccess>> GetForDoctorAsync(
        Guid doctorProfileId,
        DoctorPatientAccessStatus status,
        DateTime utcNow,
        int pageNumber,
        int pageSize,
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

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(access => access.ExpiresAt)
            .ThenBy(access => access.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<DoctorPatientAccess>(
            items,
            totalCount,
            pageNumber,
            pageSize);
    }

    public async Task<PaginatedResult<DoctorPatientAccess>> GetActiveForPatientAsync(
        Guid patientProfileId,
        DateTime utcNow,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.DoctorPatientAccesses
            .AsNoTracking()
            .Include(access => access.Doctor)
                .ThenInclude(doctor => doctor.User)
            .Where(access =>
                access.PatientProfileId == patientProfileId &&
                access.Status == DoctorPatientAccessStatus.Active &&
                access.ExpiresAt > utcNow);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(access => access.ExpiresAt)
            .ThenBy(access => access.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<DoctorPatientAccess>(
            items,
            totalCount,
            pageNumber,
            pageSize);
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

    public Task<int> RevokeRedeemedRequestForAccessAsync(
        Guid accessId,
        Guid patientProfileId,
        DateTime revokedAt,
        CancellationToken cancellationToken)
    {
        return dbContext.PatientAccessRequests
            .Where(request =>
                request.PatientProfileId == patientProfileId &&
                request.Status == PatientAccessRequestStatus.Redeemed &&
                dbContext.DoctorPatientAccesses.Any(access =>
                    access.Id == accessId &&
                    access.PatientProfileId == patientProfileId &&
                    access.PatientAccessRequestId == request.Id))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        request => request.Status,
                        PatientAccessRequestStatus.Revoked)
                    .SetProperty(request => request.UpdatedAt, revokedAt),
                cancellationToken);
    }

    public Task<int> ExpireActiveAccessesAsync(
        Guid? doctorProfileId,
        Guid? patientProfileId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        return dbContext.DoctorPatientAccesses
            .Where(access =>
                (!doctorProfileId.HasValue ||
                 access.DoctorProfileId == doctorProfileId.Value) &&
                (!patientProfileId.HasValue ||
                 access.PatientProfileId == patientProfileId.Value) &&
                access.Status == DoctorPatientAccessStatus.Active &&
                access.ExpiresAt <= utcNow)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        access => access.Status,
                        DoctorPatientAccessStatus.Expired)
                    .SetProperty(access => access.UpdatedAt, utcNow),
                cancellationToken);
    }

    public Task<int> ExpireRedeemedRequestsForExpiredAccessesAsync(
        Guid? doctorProfileId,
        Guid? patientProfileId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        return dbContext.PatientAccessRequests
            .Where(request =>
                request.Status == PatientAccessRequestStatus.Redeemed &&
                dbContext.DoctorPatientAccesses.Any(access =>
                    access.PatientAccessRequestId == request.Id &&
                    (!doctorProfileId.HasValue ||
                     access.DoctorProfileId == doctorProfileId.Value) &&
                    (!patientProfileId.HasValue ||
                     access.PatientProfileId == patientProfileId.Value) &&
                    access.Status == DoctorPatientAccessStatus.Expired &&
                    access.ExpiresAt <= utcNow))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        request => request.Status,
                        PatientAccessRequestStatus.Expired)
                    .SetProperty(request => request.UpdatedAt, utcNow),
                cancellationToken);
    }

    public Task<int> RevokeAllActiveForDoctorAsync(
        Guid doctorProfileId,
        DateTime revokedAt,
        CancellationToken cancellationToken)
    {
        return dbContext.DoctorPatientAccesses
            .Where(access =>
                access.DoctorProfileId == doctorProfileId &&
                access.Status == DoctorPatientAccessStatus.Active)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        access => access.Status,
                        DoctorPatientAccessStatus.Revoked)
                    .SetProperty(access => access.RevokedAt, revokedAt)
                    .SetProperty(access => access.UpdatedAt, revokedAt),
                cancellationToken);
    }

    public async Task<bool> HasActiveAccessAsync(
    Guid doctorProfileId,
    Guid patientProfileId,
    DateTime utcNow,
    CancellationToken cancellationToken)
    {
        return await dbContext.DoctorPatientAccesses
            .AsNoTracking()
            .AnyAsync(x =>
                x.DoctorProfileId == doctorProfileId &&
                x.PatientProfileId == patientProfileId &&
                x.Status == DoctorPatientAccessStatus.Active &&
                x.RevokedAt == null &&
                x.ExpiresAt > utcNow,
                cancellationToken);
    }
}
