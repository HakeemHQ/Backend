using Hakeem.Api.Validation;
using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Features.PushDevices.Commands.RegisterPushDevice;
using Hakeem.Application.Features.PushDevices.Commands.UnregisterPushDevice;
using Hakeem.Application.Resources;
using Hakeem.Domain.Enums.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Hakeem.Api.Controllers;

[ApiController]
[Route("patient/push-devices")]
[Authorize(Roles = nameof(ApplicationRole.Patient))]
public sealed class PatientPushDevicesController(
    IMediator mediator,
    IStringLocalizer<SharedResource> localizer)
    : ApiControllerBase(localizer)
{
    [HttpPut]
    [ProducesResponseType(typeof(GenericResponseModel<RegisterPushDeviceResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status422UnprocessableEntity)]
    [ValidationStatusCode(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RegisterOrUpdate(
        [FromBody] RegisterPushDeviceCommand command,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(command, cancellationToken);
        return SuccessResponse(response, "PushDevice.Registered");
    }

    [HttpDelete]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status422UnprocessableEntity)]
    [ValidationStatusCode(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Unregister(
        [FromBody] UnregisterPushDeviceCommand command,
        CancellationToken cancellationToken)
    {
        await mediator.Send(command, cancellationToken);
        return SuccessResponse("PushDevice.Unregistered");
    }
}
