using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Repositories.PatientProfiles;
using MediatR;

namespace Hakeem.Application.Features.PatientProfile.Commands.UpdateProfile;

public sealed class UpdateProfileCommandHandler(
    IPatientProfileRepository patientProfileRepository,
    ICurrentUserContext currentUserContext)
    : IRequestHandler<UpdateProfileCommand, UpdateProfileResult>
{
    public async Task<UpdateProfileResult> Handle(
        UpdateProfileCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await patientProfileRepository.UpdateByUserIdAsync(
            currentUserContext.UserId,
            request.FullName,
            request.BirthDate,
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            request.Gender,
            cancellationToken);

        if (profile is null)
        {
            throw new UnAuthorizedException("Profile.NotFound");
        }

        return new UpdateProfileResult(
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
