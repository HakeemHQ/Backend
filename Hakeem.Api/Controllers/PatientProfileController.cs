using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.DTOs.PatientProfiles;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Application.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Hakeem.Api.Controllers;

[ApiController]
[Route("")]
public class PatientProfileController : ApiControllerBase
{
    private readonly IPatientProfileRepository _patientProfileRepository;
    private readonly ICurrentUserContext _currentUserContext;

    public PatientProfileController(
        IStringLocalizer<SharedResource> localizer,
        IPatientProfileRepository patientProfileRepository,
        ICurrentUserContext currentUserContext)
        : base(localizer)
    {
        _patientProfileRepository = patientProfileRepository;
        _currentUserContext = currentUserContext;
    }

    // [AllowAnonymous]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(
        CancellationToken cancellationToken)
    {
        var profile = await _patientProfileRepository.GetByUserIdAsync(
            _currentUserContext.UserId,
            cancellationToken);

        if (profile is null)
        {
            var message = Localizer["Profile.NotFound"].Value;

            return Unauthorized(
                GenericResponseModel<object>.Failure(
                    message,
                    "Profile.NotFound"));
        }

        var response = BuildProfileResponse(profile);

        return SuccessResponse(response, "Profile.Retrieved");
    }

    // [AllowAnonymous]
    [HttpPatch("profile")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdatePatientProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(
                GenericResponseModel<object>.Failure(
                    Localizer["Profile.InvalidRequest"].Value,
                    "Profile.InvalidRequest"));
        }

        if (request.FullName is null && request.BirthDate is null)
        {
            return BadRequest(
                GenericResponseModel<object>.Failure(
                    Localizer["Profile.InvalidRequest"].Value,
                    "Profile.InvalidRequest"));
        }

        if (request.FullName is not null &&
            string.IsNullOrWhiteSpace(request.FullName))
        {
            return UnprocessableEntity(
                GenericResponseModel<object>.Failure(
                    Localizer["Profile.ValidationFailed"].Value,
                    "Profile.ValidationFailed"));
        }

        if (request.BirthDate is not null &&
            request.BirthDate.Value > DateTime.Today)
        {
            return UnprocessableEntity(
                GenericResponseModel<object>.Failure(
                    Localizer["Profile.ValidationFailed"].Value,
                    "Profile.ValidationFailed"));
        }

        var profile = await _patientProfileRepository.UpdateByUserIdAsync(
            _currentUserContext.UserId,
            request,
            cancellationToken);

        if (profile is null)
        {
            var message = Localizer["Profile.NotFound"].Value;

            return Unauthorized(
                GenericResponseModel<object>.Failure(
                    message,
                    "Profile.NotFound"));
        }

        var response = BuildProfileResponse(profile);

        return SuccessResponse(response, "Profile.Updated");
    }

    private static object BuildProfileResponse(
        Hakeem.Domain.Entities.PatientProfile profile)
    {
        return new
        {
            userId = profile.UserId,
            email = profile.User.Email,
            fullName = profile.FullName,
            birthDate = profile.BirthDate.ToString("yyyy-MM-dd"),
            status = profile.User.Status
        };
    }
}
