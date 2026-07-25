using Hakeem.Api.Controllers;
using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Resources;
using Hakeem.Application.Services.Auth;
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

    public AuthController(IMediator mediator, IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _mediator = mediator;
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

        return Ok(GenericResponseModel<object>.Success(null!, Localizer["Auth.PasswordResetSuccess"].Value));
    }
}
