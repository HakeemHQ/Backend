using Hakeem.Api.Controllers;
using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Resources;
using Hakeem.Application.Features.Auth.Commands.PasswordReset.Confirm;
using Hakeem.Application.Features.Auth.Commands.PasswordReset.Request;
using Hakeem.Application.Features.Auth.Commands.Registration;
using Hakeem.Application.Features.Auth.Commands.Login;
using Hakeem.Application.Features.Auth.Commands.Logout;
using Hakeem.Application.Features.Auth.Commands.Refresh;
using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Hakeem.Application.Common.Interfaces;

namespace Hakeem.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly IValidator<RegisterCommand> _registerValidator;
    private readonly IValidator<LoginCommand> _loginValidator;
    private readonly IValidator<RefreshCommand> _refreshValidator;

    private readonly ICurrentUserContext _currentUserContext;

    public AuthController(
        IMediator mediator,
        IValidator<RegisterCommand> registerValidator,
        IValidator<LoginCommand> loginValidator,
        IValidator<RefreshCommand> refreshValidator,
        IStringLocalizer<SharedResource> localizer,
        ICurrentUserContext currentUser)
        : base(localizer)
    {
        _mediator = mediator;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _refreshValidator = refreshValidator;
        _currentUserContext = currentUser;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(GenericResponseModel<LoginResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody, CustomizeValidator(Skip = true)] LoginCommand request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(GenericResponseModel<object>.Failure(
                Localizer["Validation.InvalidRequest"].Value,
                "Validation.InvalidRequest"));
        }

        var validationResult = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(error => ErrorResponseModel.Create(
                    error.PropertyName,
                    error.ErrorMessage,
                    error.ErrorCode))
                .ToList();

            return BadRequest(GenericResponseModel<object>.Failure(
                Localizer["Validation.Error"].Value,
                errors));
        }

        var result = await _mediator.Send(request, cancellationToken);
        return SuccessResponse(result, "Auth.LoggedIn");
    }

    [AllowAnonymous]
    [HttpPost("/refresh")]
    [ProducesResponseType(typeof(GenericResponseModel<RefreshResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody, CustomizeValidator(Skip = true)] RefreshCommand request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(GenericResponseModel<object>.Failure(
                Localizer["Validation.InvalidRequest"].Value,
                "Validation.InvalidRequest"));
        }

        var validationResult = await _refreshValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(error => ErrorResponseModel.Create(
                    error.PropertyName,
                    error.ErrorMessage,
                    error.ErrorCode))
                .ToList();

            return BadRequest(GenericResponseModel<object>.Failure(
                Localizer["Validation.Error"].Value,
                errors));
        }

        var result = await _mediator.Send(request, cancellationToken);
        return SuccessResponse(result, "Auth.TokenRefreshed");
    }

    [Authorize(Roles = "Patient,Doctor,Admin")]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(
     typeof(GenericResponseModel<object>),
     StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(
     CancellationToken cancellationToken)
    {
        var userId = _currentUserContext.UserId;
        var jwtId = User.FindFirstValue(JwtRegisteredClaimNames.Jti);

        if (string.IsNullOrWhiteSpace(jwtId))
        {
            return Unauthorized();
        }

        await _mediator.Send(
            new LogoutCommand(userId, jwtId),
            cancellationToken);

        return NoContent();
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
