using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Features.DoctorPatientAccesses.Commands.RevokeDoctorPatientAccess;
using Hakeem.Application.Features.DoctorPatientAccesses.Queries.GetPatientDoctorAccesses;
using Hakeem.Application.Resources;
using Hakeem.Domain.Enums.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Hakeem.Api.Controllers;

[ApiController]
[Route("doctor-access")]
[Authorize(Roles = nameof(ApplicationRole.Patient))]
public sealed class DoctorAccessController(
    IMediator mediator,
    IStringLocalizer<SharedResource> localizer)
    : ApiControllerBase(localizer)
{
    [HttpGet]
    [ProducesResponseType(typeof(GenericResponseModel<GetPatientDoctorAccessesResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(
            new GetPatientDoctorAccessesQuery(),
            cancellationToken);
        return SuccessResponse(response, "PatientAccess.PatientAccessesRetrieved");
    }

    [HttpDelete("{accessId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(
        Guid accessId,
        CancellationToken cancellationToken)
    {
        await mediator.Send(
            new RevokeDoctorPatientAccessCommand(accessId),
            cancellationToken);
        return NoContent();
    }
}
