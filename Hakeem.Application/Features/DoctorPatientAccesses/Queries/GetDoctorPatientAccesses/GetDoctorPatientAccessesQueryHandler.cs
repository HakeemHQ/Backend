using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Repositories.DoctorPatientAccesses;
using Hakeem.Application.Repositories.DoctorProfiles;
using Hakeem.Domain.Enums.Identity;
using MediatR;

namespace Hakeem.Application.Features.DoctorPatientAccesses.Queries.GetDoctorPatientAccesses;

public sealed class GetDoctorPatientAccessesQueryHandler(
    IDoctorPatientAccessRepository accessRepository,
    IDoctorProfileRepository doctorProfileRepository,
    ICurrentUserContext currentUserContext)
    : IRequestHandler<GetDoctorPatientAccessesQuery, GetDoctorPatientAccessesResult>
{
    public async Task<GetDoctorPatientAccessesResult> Handle(
        GetDoctorPatientAccessesQuery request,
        CancellationToken cancellationToken)
    {
        var doctor = await doctorProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (doctor is null)
        {
            throw new UnAuthorizedException(ErrorCodes.AuthUnauthorized);
        }

        if (doctor.User.Status != AccountStatus.Active)
        {
            throw new UnAuthorizedException(ErrorCodes.AuthAccountInactive);
        }

        var accesses = await accessRepository.GetForDoctorAsync(
            doctor.Id,
            request.Status,
            DateTime.UtcNow,
            cancellationToken);

        return new GetDoctorPatientAccessesResult(
            accesses.Select(access => new DoctorPatientAccessItem(
                    access.Id,
                    access.PatientProfileId,
                    access.Patient.PatientCode,
                    access.Patient.FullName,
                    access.ExpiresAt))
                .ToList());
    }
}
