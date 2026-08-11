using Hakeem.Api.Validation;
using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Features.PatientAccessRequests.Commands.ApprovePatientAccessRequest;
using Hakeem.Application.Features.PatientAccessRequests.Commands.RejectPatientAccessRequest;
using Hakeem.Application.Features.PatientAccessRequests.Queries.GetPatientAccessRequests;
using Hakeem.Application.Resources;
using Hakeem.Domain.Enums.Access;
using Hakeem.Domain.Enums.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Hakeem.Api.Controllers;

[ApiController]
[Route("patient-access-requests")]
[Authorize(Roles = nameof(ApplicationRole.Patient))]
public sealed class PatientAccessRequestsController(
    IMediator mediator,
    IStringLocalizer<SharedResource> localizer)
    : ApiControllerBase(localizer)
{
    [HttpGet]
    [ProducesResponseType(typeof(GenericResponseModel<GetPatientAccessRequestsResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status422UnprocessableEntity)]
    [ValidationStatusCode(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Get(
        [FromQuery] PatientAccessRequestStatus? status,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(
            new GetPatientAccessRequestsQuery(status),
            cancellationToken);
        return SuccessResponse(response, "PatientAccess.RequestsRetrieved");
    }

    [HttpPost("{requestId:guid}/approve")]
    [ProducesResponseType(typeof(GenericResponseModel<ApprovePatientAccessRequestResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Approve(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(
            new ApprovePatientAccessRequestCommand(requestId),
            cancellationToken);
        return SuccessResponse(response, "PatientAccess.RequestApproved");
    }

    [HttpPost("{requestId:guid}/reject")]
    [ProducesResponseType(typeof(GenericResponseModel<RejectPatientAccessRequestResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reject(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(
            new RejectPatientAccessRequestCommand(requestId),
            cancellationToken);
        return SuccessResponse(response, "PatientAccess.RequestRejected");
    }
}
