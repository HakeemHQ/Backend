using Hakeem.Api.Authorization;
using Hakeem.Application.Common;
using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Features.Doctor.MedicalRecords.Queries.GetMedicalRecordsByDocument;
using Hakeem.Application.Features.MedicalRecords.DTOs;
using Hakeem.Application.Resources;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Hakeem.Api.Controllers;

[ApiController]
[Route("doctor/patients/medical-records")]
[Authorize(Policy = PatientResourceAccessPolicy.DoctorOnly)]
public sealed class DoctorDocumentMedicalRecordsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public DoctorDocumentMedicalRecordsController(
        IMediator mediator,
        IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _mediator = mediator;
    }

    [HttpGet("{documentId:guid}")]
    [ProducesResponseType(
        typeof(GenericResponseModel<PaginatedResult<MedicalRecordDto>>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMedicalRecords(
        Guid documentId,
        [FromQuery] string? search,
        [FromQuery] string? recordType,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetMedicalRecordsByDocumentQuery(
                documentId,
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
