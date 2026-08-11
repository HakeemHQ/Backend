using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Features.DoctorPatientAccesses.Queries.GetDoctorPatientAccesses;
using Hakeem.Application.Resources;
using Hakeem.Domain.Enums.Access;
using Hakeem.Domain.Enums.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Hakeem.Api.Controllers;

[ApiController]
[Route("doctor/patient-access")]
[Authorize(Roles = nameof(ApplicationRole.Doctor))]
public sealed class DoctorPatientAccessController(
    IMediator mediator,
    IStringLocalizer<SharedResource> localizer)
    : ApiControllerBase(localizer)
{
    [HttpGet]
    [ProducesResponseType(typeof(GenericResponseModel<GetDoctorPatientAccessesResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Get(
        [FromQuery] DoctorPatientAccessStatus status = DoctorPatientAccessStatus.Active,
        CancellationToken cancellationToken = default)
    {
        var response = await mediator.Send(
            new GetDoctorPatientAccessesQuery(status),
            cancellationToken);
        return SuccessResponse(response, "PatientAccess.DoctorAccessesRetrieved");
    }
}
