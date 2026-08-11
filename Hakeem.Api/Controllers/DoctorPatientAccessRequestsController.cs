using Hakeem.Api.Validation;
using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Features.PatientAccessRequests.Commands.CreatePatientAccessRequest;
using Hakeem.Application.Resources;
using Hakeem.Domain.Enums.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Hakeem.Api.Controllers;

[ApiController]
[Route("doctor/patient-access-requests")]
[Authorize(Roles = nameof(ApplicationRole.Doctor))]
public sealed class DoctorPatientAccessRequestsController(
    IMediator mediator,
    IStringLocalizer<SharedResource> localizer)
    : ApiControllerBase(localizer)
{
    [HttpPost]
    [ProducesResponseType(typeof(GenericResponseModel<CreatePatientAccessRequestResult>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status422UnprocessableEntity)]
    [ValidationStatusCode(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePatientAccessRequestCommand request,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(request, cancellationToken);
        return CreatedResponse(response, "PatientAccess.RequestCreated");
    }
}
