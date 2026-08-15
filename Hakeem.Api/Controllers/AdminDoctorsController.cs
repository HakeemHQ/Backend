using Hakeem.Api.Validation;
using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Features.Admin.ActiveSummery.Queries;
using Hakeem.Application.Features.Admin.ActiveSummery.DTOs;
using Hakeem.Application.Features.Admin.AduitLogs.GetAuditLogs.DTOs;
using Hakeem.Application.Features.Admin.AduitLogs.GetAuditLogs.Queries;
using Hakeem.Application.Features.Admin.Doctors.Commands.AddDoctor;
using Hakeem.Application.Features.Admin.Doctors.Commands.UpdateDoctorStatus;
using Hakeem.Application.Features.Admin.Doctors.Commands.UpdateDoctorStatus.DTOs;
using Hakeem.Application.Features.Admin.Doctors.DTOs;
using Hakeem.Application.Features.Admin.Doctors.Queries;
using Hakeem.Application.Features.Admin.Doctors.Queries.GetDoctorById;
using Hakeem.Application.Features.Admin.Users.GetUsers.Queries;
using Hakeem.Application.Features.Admin.Users.GetUsers.DTOs;
using Hakeem.Application.Features.Admin.Users.UpdateUserStatus.Commands;
using Hakeem.Application.Features.Admin.Users.UpdateUserStatus.DTOs;
using Hakeem.Application.Resources;
using Hakeem.Domain.Enums.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Hakeem.Api.Controllers
{
    [ApiController]
    [Route("admin")]
    [Authorize(Roles = nameof(ApplicationRole.Admin))]
    public class AdminDoctorsController(IMediator mediator, IStringLocalizer<SharedResource> localizer) : ApiControllerBase(localizer)
    {
        [HttpPost("doctors")]
        [ValidationStatusCode(StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(
            typeof(GenericResponseModel<CreateDoctorResponse>),
            StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> CreateDoctor(
            [FromBody] CreateDoctorCommand command,
            CancellationToken cancellationToken)
        {
            var result = await mediator.Send(command,cancellationToken);
            return CreatedResponse(result);
        }

        [HttpGet("doctors")]
        [ProducesResponseType(
            typeof(GenericResponseModel<AdminDoctorsResponse>),
            StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDoctors(
            [FromQuery] GetDoctorsQuery query,
            CancellationToken cancellationToken)
        {
            var result = await mediator.Send(query, cancellationToken);
            return SuccessResponse(result);
        }

        [HttpGet("doctors/{doctorId:guid}")]
        [ProducesResponseType(
            typeof(GenericResponseModel<AdminDoctorResponse>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDoctorById(
            Guid doctorId,
            CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new GetDoctorByIdQuery(doctorId),cancellationToken);
            return SuccessResponse(result);
        }


        [HttpPatch("doctors/{doctorId:guid}/status")]
        [ValidationStatusCode(StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(
            typeof(GenericResponseModel<UpdateDoctorStatusResult>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> UpdateDoctorStatus(Guid doctorId,
           [FromBody] UpdateDoctorStatusRequest request,CancellationToken cancellationToken)
        {
            var result = await mediator.Send(
                new UpdateDoctorStatusCommand(doctorId, request.Status),
                cancellationToken);
            return SuccessResponse(result);
        }


        [HttpGet("users")]
        [ProducesResponseType(
            typeof(GenericResponseModel<AdminUsersResponse>),
            StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUsers(
            [FromQuery] GetAdminUsersQuery query,
            CancellationToken cancellationToken)
        {
            var result = await mediator.Send(query, cancellationToken);
            return SuccessResponse(result);
        }

        [HttpPatch("users/{userId:guid}/status")]
        [ValidationStatusCode(StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(
            typeof(GenericResponseModel<UpdateUserStatusResult>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> UpdateUserStatus(
            Guid userId,
            [FromBody] UpdateUserStatusRequest request,
            CancellationToken cancellationToken)
        {
            var command = new UpdateUserStatusCommand(userId,request.Status);
            var result = await mediator.Send(command, cancellationToken);
            return SuccessResponse(result);
        }

        [HttpGet("audit-logs")]
        [ProducesResponseType(
            typeof(GenericResponseModel<AdminAuditLogsResponse>),
            StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] GetAdminAuditLogsQuery query,
            CancellationToken cancellationToken)
        {
            var result = await mediator.Send(query, cancellationToken);
            return SuccessResponse(result);
           
        }


        [HttpGet("activity-summary")]
        [ProducesResponseType(
            typeof(GenericResponseModel<ActivitySummaryDto>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> GetActivitySummary([FromQuery] DateOnly? fromDate,[FromQuery] DateOnly? toDate,
                                                             CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new GetActivitySummaryQuery(fromDate, toDate),cancellationToken);
            return SuccessResponse(result);
        }
    }
}
