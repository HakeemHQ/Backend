using Hakeem.Api.Authorization;
using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Features.MedicalIntelligence.Commands.Chat;
using Hakeem.Application.Resources;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Hakeem.Api.Controllers;

[Route("medical-intelligence")]
[Authorize(Policy = VerifiedPatientPolicy.Name)]
public sealed class MedicalIntelligenceController(
    IMediator mediator,
    IStringLocalizer<SharedResource> localizer)
    : ApiControllerBase(localizer)
{
    [HttpPost("chat")]
    [ProducesResponseType(
        typeof(GenericResponseModel<MedicalIntelligenceChatResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Chat(
        [FromBody] MedicalIntelligenceChatCommand request,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(request, cancellationToken);
        return SuccessResponse(response);
    }
}
