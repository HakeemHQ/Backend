using Hakeem.Application.Common;
using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Repositories.DoctorPatientAccesses;
using Hakeem.Application.Repositories.PatientProfiles;
using MediatR;

namespace Hakeem.Application.Features.DoctorPatientAccesses.Queries.GetPatientDoctorAccesses;

public sealed class GetPatientDoctorAccessesQueryHandler(
    IDoctorPatientAccessRepository accessRepository,
    IPatientProfileRepository patientProfileRepository,
    ICurrentUserContext currentUserContext)
    : IRequestHandler<GetPatientDoctorAccessesQuery, PaginatedResult<PatientDoctorAccessItem>>
{
    public async Task<PaginatedResult<PatientDoctorAccessItem>> Handle(
        GetPatientDoctorAccessesQuery request,
        CancellationToken cancellationToken)
    {
        var patient = await patientProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (patient is null)
        {
            throw new UnAuthorizedException(ErrorCodes.AuthUnauthorized);
        }

        var accesses = await accessRepository.GetActiveForPatientAsync(
            patient.Id,
            DateTime.UtcNow,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        return new PaginatedResult<PatientDoctorAccessItem>(
            accesses.Items.Select(access => new PatientDoctorAccessItem(
                    access.Id,
                    access.DoctorProfileId,
                    $"{access.Doctor.User.FirstName} {access.Doctor.User.LastName}".Trim(),
                    access.Doctor.Specialty,
                    access.ExpiresAt)),
            accesses.TotalCount,
            accesses.PageNumber,
            accesses.PageSize);
    }
}
