using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.PatientProfile.DTOs;
using Hakeem.Application.Repositories.PatientProfiles;
using MediatR;

namespace Hakeem.Application.Features.PatientProfile.Queries.GetProfile;

public sealed class GetProfileQueryHandler(
    IPatientProfileRepository patientProfileRepository,
    ICurrentUserContext currentUserContext)
    : IRequestHandler<GetProfileQuery, PatientProfileResponse>
{
    public async Task<PatientProfileResponse> Handle(
        GetProfileQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await patientProfileRepository.GetByUserIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (profile is null)
        {
            throw new UnAuthorizedException("Profile.NotFound");
        }

        return new PatientProfileResponse(
            profile.UserId,
            profile.User.Email,
            profile.FullName,
            profile.BirthDate.ToString("yyyy-MM-dd"),
            profile.User.Status,
            profile.User.FirstName,
            profile.User.LastName,
            profile.User.PhoneNumber,
            profile.User.Gender);
    }
}
