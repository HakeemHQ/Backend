using Hakeem.Api.Validation;
using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Features.PatientIdentities.Commands.VerifyPatientIdentity;
using Hakeem.Application.Resources;
using Hakeem.Domain.Enums.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Hakeem.Api.Controllers;

[ApiController]
[Route("doctor/patient-identities")]
[Authorize(Roles = nameof(ApplicationRole.Doctor))]
public sealed class DoctorPatientIdentitiesController(
    IMediator mediator,
    IStringLocalizer<SharedResource> localizer)
    : ApiControllerBase(localizer)
{
    [HttpPost("verify")]
    [ProducesResponseType(typeof(GenericResponseModel<VerifyPatientIdentityResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status422UnprocessableEntity)]
    [ValidationStatusCode(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Verify(
        [FromBody] VerifyPatientIdentityCommand request,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(request, cancellationToken);
        return SuccessResponse(response, "PatientIdentity.Verified");
    }
}
