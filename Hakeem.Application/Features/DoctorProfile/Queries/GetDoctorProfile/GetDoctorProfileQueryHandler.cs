using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.DoctorProfile.DTOs;
using Hakeem.Application.Repositories.DoctorProfiles;
using MediatR;

namespace Hakeem.Application.Features.DoctorProfile.Queries.GetDoctorProfile;

public sealed class GetDoctorProfileQueryHandler(
    IDoctorProfileRepository doctorProfileRepository,
    ICurrentUserContext currentUserContext)
    : IRequestHandler<GetDoctorProfileQuery, DoctorProfileResponse>
{
    public async Task<DoctorProfileResponse> Handle(
        GetDoctorProfileQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await doctorProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (profile is null)
        {
            throw new UnAuthorizedException("Profile.NotFound");
        }

        return new DoctorProfileResponse(
            profile.Id,
            $"{profile.User.FirstName} {profile.User.LastName}".Trim(),
            profile.User.Email,
            profile.Specialty,
            profile.LicenseNumber,
            profile.User.Status.ToString());
    }
}
