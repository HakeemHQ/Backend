using Hakeem.Application.Common;
using Hakeem.Application.Repositories.PatientAccessRequests;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Access;
using Hakeem.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Hakeem.Infrastructure.Repositories.PatientAccessRequests;

public sealed class PatientAccessRequestRepository(ApplicationDbContext dbContext)
    : IPatientAccessRequestRepository
{
    public Task<PatientProfile?> GetPatientByCodeAsync(
        string patientCode,
        CancellationToken cancellationToken)
    {
        return dbContext.PatientProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(
                patient => patient.PatientCode == patientCode,
                cancellationToken);
    }

    public Task<int> ExpireStaleRequestsAsync(
        Guid patientProfileId,
        DateTime pendingExpiresBefore,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        return dbContext.PatientAccessRequests
            .Where(request =>
                request.PatientProfileId == patientProfileId &&
                ((request.Status == PatientAccessRequestStatus.Pending &&
                  request.RequestedAt <= pendingExpiresBefore) ||
                 (request.Status == PatientAccessRequestStatus.Approved &&
                  request.CodeExpiresAt <= utcNow)))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        request => request.Status,
                        PatientAccessRequestStatus.Expired)
                    .SetProperty(request => request.UpdatedAt, utcNow),
                cancellationToken);
    }

    public Task<bool> HasBlockingRequestAsync(
        Guid doctorProfileId,
        Guid patientProfileId,
        DateTime pendingExpiresBefore,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        return dbContext.PatientAccessRequests.AnyAsync(
            request =>
                request.DoctorProfileId == doctorProfileId &&
                request.PatientProfileId == patientProfileId &&
                ((request.Status == PatientAccessRequestStatus.Pending &&
                  request.RequestedAt > pendingExpiresBefore) ||
                 (request.Status == PatientAccessRequestStatus.Approved &&
                  request.CodeExpiresAt > utcNow)),
            cancellationToken);
    }

    public Task<bool> HasActiveAccessAsync(
        Guid doctorProfileId,
        Guid patientProfileId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        return dbContext.DoctorPatientAccesses.AnyAsync(
            access =>
                access.DoctorProfileId == doctorProfileId &&
                access.PatientProfileId == patientProfileId &&
                access.Status == DoctorPatientAccessStatus.Active &&
                access.ExpiresAt > utcNow,
            cancellationToken);
    }

    public void Add(PatientAccessRequest accessRequest)
    {
        dbContext.PatientAccessRequests.Add(accessRequest);
    }

    public async Task<PaginatedResult<PatientAccessRequest>> GetForPatientAsync(
        Guid patientProfileId,
        PatientAccessRequestStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.PatientAccessRequests
            .AsNoTracking()
            .Include(request => request.Doctor)
                .ThenInclude(doctor => doctor.User)
            .Where(request => request.PatientProfileId == patientProfileId);

        if (status.HasValue)
        {
            query = query.Where(request => request.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(request => request.RequestedAt)
            .ThenByDescending(request => request.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<PatientAccessRequest>(
            items,
            totalCount,
            pageNumber,
            pageSize);
    }

    public Task<PatientAccessRequest?> GetByIdForPatientAsync(
        Guid requestId,
        Guid patientProfileId,
        CancellationToken cancellationToken)
    {
        return dbContext.PatientAccessRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(
                request =>
                    request.Id == requestId &&
                    request.PatientProfileId == patientProfileId,
                cancellationToken);
    }

    public Task<int> ApprovePendingAsync(
        Guid requestId,
        Guid patientProfileId,
        string codeHash,
        DateTime approvedAt,
        DateTime codeExpiresAt,
        DateTime pendingExpiresBefore,
        CancellationToken cancellationToken)
    {
        return dbContext.PatientAccessRequests
            .Where(request =>
                request.Id == requestId &&
                request.PatientProfileId == patientProfileId &&
                request.Status == PatientAccessRequestStatus.Pending &&
                request.RequestedAt > pendingExpiresBefore)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        request => request.Status,
                        PatientAccessRequestStatus.Approved)
                    .SetProperty(request => request.ApprovedAt, approvedAt)
                    .SetProperty(request => request.CodeHash, codeHash)
                    .SetProperty(request => request.CodeExpiresAt, codeExpiresAt)
                    .SetProperty(request => request.UpdatedAt, approvedAt),
                cancellationToken);
    }

    public Task<int> RejectPendingAsync(
        Guid requestId,
        Guid patientProfileId,
        DateTime rejectedAt,
        DateTime pendingExpiresBefore,
        CancellationToken cancellationToken)
    {
        return dbContext.PatientAccessRequests
            .Where(request =>
                request.Id == requestId &&
                request.PatientProfileId == patientProfileId &&
                request.Status == PatientAccessRequestStatus.Pending &&
                request.RequestedAt > pendingExpiresBefore)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        request => request.Status,
                        PatientAccessRequestStatus.Rejected)
                    .SetProperty(request => request.UpdatedAt, rejectedAt),
                cancellationToken);
    }

    public async Task<IReadOnlyList<PatientAccessRequest>> GetCodeCandidatesAsync(
        Guid doctorProfileId,
        Guid patientProfileId,
        CancellationToken cancellationToken)
    {
        return await dbContext.PatientAccessRequests
            .AsNoTracking()
            .Where(request =>
                request.DoctorProfileId == doctorProfileId &&
                request.PatientProfileId == patientProfileId &&
                request.CodeHash != null &&
                (request.Status == PatientAccessRequestStatus.Approved ||
                 request.Status == PatientAccessRequestStatus.Redeemed))
            .OrderByDescending(request => request.ApprovedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<int> ExpireStaleActiveAccessAsync(
        Guid doctorProfileId,
        Guid patientProfileId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        return dbContext.DoctorPatientAccesses
            .Where(access =>
                access.DoctorProfileId == doctorProfileId &&
                access.PatientProfileId == patientProfileId &&
                access.Status == DoctorPatientAccessStatus.Active &&
                access.ExpiresAt <= utcNow)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(access => access.Status, DoctorPatientAccessStatus.Expired)
                    .SetProperty(access => access.UpdatedAt, utcNow),
                cancellationToken);
    }

    public Task<int> RedeemApprovedAsync(
        Guid requestId,
        Guid doctorProfileId,
        Guid patientProfileId,
        DateTime redeemedAt,
        CancellationToken cancellationToken)
    {
        return dbContext.PatientAccessRequests
            .Where(request =>
                request.Id == requestId &&
                request.DoctorProfileId == doctorProfileId &&
                request.PatientProfileId == patientProfileId &&
                request.Status == PatientAccessRequestStatus.Approved &&
                request.CodeExpiresAt > redeemedAt)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(request => request.Status, PatientAccessRequestStatus.Redeemed)
                    .SetProperty(request => request.RedeemedAt, redeemedAt)
                    .SetProperty(request => request.UpdatedAt, redeemedAt),
                cancellationToken);
    }

    public Task<int> ExpireApprovedAsync(
        Guid requestId,
        Guid doctorProfileId,
        Guid patientProfileId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        return dbContext.PatientAccessRequests
            .Where(request =>
                request.Id == requestId &&
                request.DoctorProfileId == doctorProfileId &&
                request.PatientProfileId == patientProfileId &&
                request.Status == PatientAccessRequestStatus.Approved &&
                request.CodeExpiresAt <= utcNow)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        request => request.Status,
                        PatientAccessRequestStatus.Expired)
                    .SetProperty(request => request.UpdatedAt, utcNow),
                cancellationToken);
    }

    public void AddAccess(DoctorPatientAccess access)
    {
        dbContext.DoctorPatientAccesses.Add(access);
    }
}
