using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Constants;
using Hakeem.Application.Features.MedicalDataExtraction.DTOs;
using Hakeem.Application.Features.MedicalDataExtraction.Queries.GetExtractedFields;
using Hakeem.Application.Resources;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Hakeem.Api.Controllers;

[ApiController]
[Route("documents")]
[Authorize(Roles = "Patient")]
public sealed class MedicalDataExtractionController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public MedicalDataExtractionController(
        IMediator mediator,
        IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _mediator = mediator;
    }

    [HttpGet("{documentId:guid}/extracted-fields")]
    [ProducesResponseType(
        typeof(GenericResponseModel<ExtractedFieldsResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExtractedFields(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetExtractedFieldsQuery(documentId),
            cancellationToken);

        return SuccessResponse(
            result,
            ErrorCodes.DocumentExtractedFieldsRetrieved);
    }
}
