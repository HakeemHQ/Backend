using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Features.PatientProfile.Commands.UpdateProfile;
using Hakeem.Application.Features.PatientProfile.DTOs;
using Hakeem.Application.Features.PatientProfile.Queries.GetProfile;
using Hakeem.Application.Resources;
using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Hakeem.Api.Controllers;

[ApiController]
[Route("")]
[Authorize(Roles = "Patient")]
public class PatientProfileController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly IValidator<UpdateProfileCommand> _updateProfileValidator;

    public PatientProfileController(
        IMediator mediator,
        IValidator<UpdateProfileCommand> updateProfileValidator,
        IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _mediator = mediator;
        _updateProfileValidator = updateProfileValidator;
    }

    [HttpGet("profile")]
    [ProducesResponseType(typeof(GenericResponseModel<PatientProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile(
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(
            new GetProfileQuery(),
            cancellationToken);
        return SuccessResponse(response, "Profile.Retrieved");
    }

    [HttpPatch("profile")]
    [ProducesResponseType(typeof(GenericResponseModel<UpdateProfileResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody, CustomizeValidator(Skip = true)] UpdateProfileCommand request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(
                GenericResponseModel<object>.Failure(
                    Localizer["Profile.InvalidRequest"].Value,
                    "Profile.InvalidRequest"));
        }

        if (request.FullName is null &&
            request.BirthDate is null &&
            request.FirstName is null &&
            request.LastName is null &&
            request.PhoneNumber is null &&
            request.Gender is null)
        {
            return BadRequest(
                GenericResponseModel<object>.Failure(
                    Localizer["Profile.InvalidRequest"].Value,
                    "Profile.InvalidRequest"));
        }

        var validationResult = await _updateProfileValidator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(error => ErrorResponseModel.Create(
                    error.PropertyName,
                    error.ErrorMessage,
                    error.ErrorCode))
                .ToList();

            return UnprocessableEntity(
                GenericResponseModel<object>.Failure(
                    Localizer["Profile.ValidationFailed"].Value,
                    errors));
        }

        var response = await _mediator.Send(
            request,
            cancellationToken);

        return SuccessResponse(response, "Profile.Updated");
    }
}
