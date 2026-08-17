using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Repositories.DoctorPatientAccesses;
using Hakeem.Application.Repositories.DoctorProfiles;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Services.Access;

public interface IDoctorPatientAccessGuard : IScoped
{
    Task<DoctorProfile> RequireDoctorWithActiveAccessAsync(
        Guid patientProfileId,
        CancellationToken cancellationToken);
}

public sealed class DoctorPatientAccessGuard(
    ICurrentUserContext currentUserContext,
    IDoctorProfileRepository doctorProfileRepository,
    IDoctorPatientAccessRepository doctorPatientAccessRepository,
    IDoctorPatientAccessExpirationService expirationService)
    : IDoctorPatientAccessGuard
{
    public async Task<DoctorProfile> RequireDoctorWithActiveAccessAsync(
        Guid patientProfileId,
        CancellationToken cancellationToken)
    {
        if (patientProfileId == Guid.Empty)
        {
            throw new ArgumentException("Patient profile ID is required.");
        }

        var doctor = await doctorProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (doctor is null)
        {
            throw new UnAuthorizedException(ErrorCodes.AuthUnauthorized);
        }

        var utcNow = DateTime.UtcNow;
        await expirationService.ExpireForPairAsync(
            doctor.Id,
            patientProfileId,
            utcNow,
            cancellationToken);

        var hasAccess = await doctorPatientAccessRepository.HasActiveAccessAsync(
            doctor.Id,
            patientProfileId,
            utcNow,
            cancellationToken);

        if (!hasAccess)
        {
            throw new ForbiddenException(
                "Doctor does not have active access to this patient.");
        }

        return doctor;
    }
}
