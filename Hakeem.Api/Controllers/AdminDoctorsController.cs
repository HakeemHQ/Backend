using Hakeem.Application.Features.Admin.ActiveSummery.Queries;
using Hakeem.Application.Features.Admin.AduitLogs.GetAuditLogs.Queries;
using Hakeem.Application.Features.Admin.Doctors.Commands.AddDoctor;
using Hakeem.Application.Features.Admin.Doctors.Commands.UpdateDoctorStatus;
using Hakeem.Application.Features.Admin.Doctors.DTOs;
using Hakeem.Application.Features.Admin.Doctors.Queries;
using Hakeem.Application.Features.Admin.Doctors.Queries.GetDoctorById;
using Hakeem.Application.Features.Admin.Users.GetUsers.Queries;
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
    [Authorize(Roles = "Admin")]
    public class AdminDoctorsController(IMediator mediator, IStringLocalizer<SharedResource> localizer) : ApiControllerBase(localizer)
    {
        [HttpPost("doctors")]
        public async Task<ActionResult<CreateDoctorResponse>> CreateDoctor(CreateDoctorCommand command,
                                                                            CancellationToken cancellationToken)
        {
            var result = await mediator.Send(command,cancellationToken);
            return Ok(result);
        }

        [HttpGet("doctors")]
        public async Task<ActionResult<AdminDoctorsResponse>> GetDoctors(
    [FromQuery] GetDoctorsQuery query,
    CancellationToken cancellationToken)
        {
            var result = await mediator.Send(query, cancellationToken);

            return Ok(result);
        }

        [HttpGet("doctors/{doctorId:guid}")]
        public async Task<ActionResult<AdminDoctorResponse>> GetDoctorById(Guid doctorId,CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new GetDoctorByIdQuery(doctorId),cancellationToken);
            return Ok(result);
        }


        [HttpPatch("doctors/{doctorId:guid}/status")]
        public async Task<IActionResult> UpdateDoctorStatus(Guid doctorId,
           [FromBody] UpdateDoctorStatusRequest request,CancellationToken cancellationToken)
        {
            await mediator.Send(new UpdateDoctorStatusCommand(doctorId,request.Status),cancellationToken);
            return Ok(new
            {
           doctorId= doctorId,
           Status= request.Status
            });
                
        }


        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] GetAdminUsersQuery query)
        {
            var result = await mediator.Send(query);
            return Ok(new
            {
                success = true,
                data = result
            });
        }

        [HttpPatch("users/{userId:guid}/status")]
        public async Task<IActionResult> UpdateUserStatus(Guid userId,[FromBody] UpdateUserStatusRequest request)
        {
            var command = new UpdateUserStatusCommand(userId,request.Status);
            var result = await mediator.Send(command);
            return Ok(new
            {
                success = true,
                data = result
            });
        }

        [HttpGet("audit-logs")]
        public async Task<IActionResult> GetAuditLogs([FromQuery] GetAdminAuditLogsQuery query)
        {
            var result = await mediator.Send(query);
            return Ok(result);
           
        }


        [HttpGet("activity-summary")]
        public async Task<IActionResult> GetActivitySummary([FromQuery] DateOnly? fromDate,[FromQuery] DateOnly? toDate,
                                                             CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new GetActivitySummaryQuery(fromDate, toDate),cancellationToken);
            return Ok (result);
        }
        public sealed record UpdateDoctorStatusRequest(AccountStatus Status);
    }
}