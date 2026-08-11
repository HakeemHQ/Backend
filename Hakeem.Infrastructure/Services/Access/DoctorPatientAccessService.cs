using Hakeem.Application.Interfaces.Access;
using Hakeem.Domain.Enums.Access;
using Hakeem.Domain.Enums.Identity;
using Hakeem.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Hakeem.Infrastructure.Services.Access;

public sealed class DoctorPatientAccessService(ApplicationDbContext dbContext)
    : IDoctorPatientAccessService
{
    public Task<bool> HasActiveAccessAsync(
        Guid doctorId,
        Guid patientId,
        CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        return dbContext.DoctorPatientAccesses
            .AsNoTracking()
            .AnyAsync(
                access =>
                    access.DoctorProfileId == doctorId &&
                    access.PatientProfileId == patientId &&
                    access.Status == DoctorPatientAccessStatus.Active &&
                    access.ExpiresAt > utcNow &&
                    access.Doctor.User.Status == AccountStatus.Active,
                cancellationToken);
    }
}
