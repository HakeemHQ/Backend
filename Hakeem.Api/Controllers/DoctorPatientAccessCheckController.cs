using Hakeem.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hakeem.Api.Controllers;

[ApiController]
[Route("doctor/patients")]
public sealed class DoctorPatientAccessCheckController : ControllerBase
{
    [HttpGet("{patientId:guid}/access-check")]
    [Authorize(Policy = DoctorPatientAccessPolicy.Name)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult CheckAccess(Guid patientId)
    {
        return NoContent();
    }
}
