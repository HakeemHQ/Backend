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

    public Task<bool> HasPendingRequestAsync(
        Guid doctorProfileId,
        Guid patientProfileId,
        CancellationToken cancellationToken)
    {
        return dbContext.PatientAccessRequests.AnyAsync(
            request =>
                request.DoctorProfileId == doctorProfileId &&
                request.PatientProfileId == patientProfileId &&
                request.Status == PatientAccessRequestStatus.Pending,
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

    public async Task<IReadOnlyList<PatientAccessRequest>> GetForPatientAsync(
        Guid patientProfileId,
        PatientAccessRequestStatus? status,
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

        return await query
            .OrderByDescending(request => request.RequestedAt)
            .ToListAsync(cancellationToken);
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
        CancellationToken cancellationToken)
    {
        return dbContext.PatientAccessRequests
            .Where(request =>
                request.Id == requestId &&
                request.PatientProfileId == patientProfileId &&
                request.Status == PatientAccessRequestStatus.Pending)
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
        CancellationToken cancellationToken)
    {
        return dbContext.PatientAccessRequests
            .Where(request =>
                request.Id == requestId &&
                request.PatientProfileId == patientProfileId &&
                request.Status == PatientAccessRequestStatus.Pending)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        request => request.Status,
                        PatientAccessRequestStatus.Rejected)
                    .SetProperty(request => request.UpdatedAt, rejectedAt),
                cancellationToken);
    }
}
