using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Features.MedicalRecords.Queries.GetMedicalRecordById;
using Hakeem.Application.Features.MedicalRecords.Queries.GetMedicalRecords;
using Hakeem.Application.Features.MedicalRecords.Queries.SearchMedicalRecords;
using Hakeem.Application.Resources;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Hakeem.Api.Controllers;

[Route("medical-records")]
public class MedicalRecordController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public MedicalRecordController(
        IMediator mediator,
        IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetMedicalRecords(
        [FromQuery] GetMedicalRecordsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return SuccessResponse(result);
    }

    [HttpGet("{medicalRecordId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetMedicalRecordById(
        Guid medicalRecordId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetMedicalRecordByIdQuery(medicalRecordId),
            cancellationToken);
        return SuccessResponse(result);
    }

    [HttpPost("search")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> SearchMedicalRecords(
        [FromBody] SearchMedicalRecordsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SearchMedicalRecordsQuery(
                request.Query,
                request.PatientProfileId,
                request.Limit),
            cancellationToken);

        return SuccessResponse(result);
    }
}

public sealed record SearchMedicalRecordsRequest(
    string Query,
    Guid PatientProfileId,
    int Limit = 10);
