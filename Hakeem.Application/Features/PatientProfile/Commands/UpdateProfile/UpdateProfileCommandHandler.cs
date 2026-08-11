using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Application.Features.PatientProfile.DTOs;
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
            profile.Id,
            profile.UserId,
            profile.PatientCode,
            profile.User.Email,
            profile.FullName,
            profile.BirthDate.ToString("yyyy-MM-dd"),
            NationalIdMasker.Mask(profile.NationalId),
            profile.IdentityVerificationStatus.ToString(),
            profile.User.Status.ToString(),
            profile.User.FirstName,
            profile.User.LastName,
            profile.User.PhoneNumber,
            profile.User.Gender);
    }
}
