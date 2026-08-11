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
}
