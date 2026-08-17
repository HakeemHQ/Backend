using Hakeem.Application.Common;
using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Repositories.DoctorPatientAccesses;
using Hakeem.Application.Repositories.DoctorProfiles;
using Hakeem.Domain.Enums.Identity;
using Hakeem.Application.Services.Access;
using MediatR;

namespace Hakeem.Application.Features.DoctorPatientAccesses.Queries.GetDoctorPatientAccesses;

public sealed class GetDoctorPatientAccessesQueryHandler(
    IDoctorPatientAccessRepository accessRepository,
    IDoctorProfileRepository doctorProfileRepository,
    ICurrentUserContext currentUserContext,
    IDoctorPatientAccessExpirationService expirationService)
    : IRequestHandler<GetDoctorPatientAccessesQuery, PaginatedResult<DoctorPatientAccessItem>>
{
    public async Task<PaginatedResult<DoctorPatientAccessItem>> Handle(
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

        var utcNow = DateTime.UtcNow;
        await expirationService.ExpireForDoctorAsync(
            doctor.Id,
            utcNow,
            cancellationToken);

        var accesses = await accessRepository.GetForDoctorAsync(
            doctor.Id,
            request.Status,
            utcNow,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        return new PaginatedResult<DoctorPatientAccessItem>(
            accesses.Items.Select(access => new DoctorPatientAccessItem(
                    access.Id,
                    access.PatientProfileId,
                    access.Patient.PatientCode,
                    access.Patient.FullName,
                    access.ExpiresAt)),
            accesses.TotalCount,
            accesses.PageNumber,
            accesses.PageSize);
    }
}
