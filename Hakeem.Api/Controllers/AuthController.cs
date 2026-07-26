using Hakeem.Api.Controllers;
using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Resources;
using Hakeem.Application.Services.Auth.PasswordReset.Confirm;
using Hakeem.Application.Services.Auth.PasswordReset.Request;
using Hakeem.Application.Services.Auth.Registration;
using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Hakeem.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly IValidator<RegisterCommand> _registerValidator;

    public AuthController(
        IMediator mediator,
        IValidator<RegisterCommand> registerValidator,
        IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _mediator = mediator;
        _registerValidator = registerValidator;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(GenericResponseModel<RegisterResult>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Register(
        [FromBody, CustomizeValidator(Skip = true)] RegisterCommand request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(GenericResponseModel<object>.Failure(
                Localizer["Validation.InvalidRequest"].Value,
                "Validation.InvalidRequest"));
        }

        var validationResult = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(error => ErrorResponseModel.Create(
                    error.PropertyName,
                    error.ErrorMessage,
                    error.ErrorCode))
                .ToList();

            return UnprocessableEntity(GenericResponseModel<object>.Failure(
                Localizer["Validation.Error"].Value,
                errors));
        }

        var result = await _mediator.Send(request, cancellationToken);
        return CreatedResponse(result, "Auth.Registered");
    }

    [AllowAnonymous]
    [HttpPost("password-reset/request")]
    public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequestCommand request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(GenericResponseModel<object>.Failure(Localizer["Validation.InvalidRequest"].Value, "Validation.InvalidRequest"));
        }

        await _mediator.Send(request, cancellationToken);

        return Accepted(GenericResponseModel<object>.Success(null!, Localizer["Auth.PasswordResetSent"].Value));
    }

    [AllowAnonymous]
    [HttpPost("password-reset/confirm")]
    public async Task<IActionResult> ConfirmPasswordReset([FromBody] PasswordResetConfirmCommand request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(GenericResponseModel<object>.Failure(Localizer["Validation.InvalidRequest"].Value, "Validation.InvalidRequest"));
        }

        await _mediator.Send(request, cancellationToken);

        return SuccessResponse("Auth.PasswordResetSuccess");
    }
}
