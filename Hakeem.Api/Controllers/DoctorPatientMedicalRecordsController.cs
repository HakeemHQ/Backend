using Hakeem.Api.Authorization;
using Hakeem.Application.Features.Doctor.MedicalRecords.Queries.GetPatientMedicalRecords;
using Hakeem.Application.Resources;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Hakeem.Api.Controllers;

[ApiController]
[Route("doctor/patients/{patientId:guid}/medical-records")]
[Authorize(Policy = DoctorPatientAccessPolicy.Name)]
public sealed class DoctorPatientMedicalRecordsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public DoctorPatientMedicalRecordsController(
        IMediator mediator,
        IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetMedicalRecords(
        Guid patientId,
        [FromQuery] string? search,
        [FromQuery] string? recordType,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetDoctorPatientMedicalRecordsQuery(
                patientId,
                search,
                recordType,
                fromDate,
                toDate,
                pageNumber,
                pageSize),
            cancellationToken);

        return SuccessResponse(result);
    }
}
