using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Features.DoctorProfile.DTOs;
using Hakeem.Application.Features.DoctorProfile.Queries.GetDoctorProfile;
using Hakeem.Application.Resources;
using Hakeem.Domain.Enums.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Hakeem.Api.Controllers;

[ApiController]
[Route("doctor/profile")]
[Authorize(Roles = nameof(ApplicationRole.Doctor))]
public sealed class DoctorProfileController(
    IMediator mediator,
    IStringLocalizer<SharedResource> localizer)
    : ApiControllerBase(localizer)
{
    [HttpGet]
    [ProducesResponseType(typeof(GenericResponseModel<DoctorProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetDoctorProfileQuery(), cancellationToken);
        return SuccessResponse(response, "Profile.Retrieved");
    }
}
