using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Features.MedicalDocuments.DTOs;
using Hakeem.Application.Features.MedicalDocuments.Queries.GetDocumentById;
using Hakeem.Application.Features.MedicalDocuments.Queries.GetDocuments;
using Hakeem.Application.Resources;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Hakeem.Api.Controllers;

[ApiController]
[Route("documents")]
[Authorize(Roles = "Patient")]
public sealed class MedicalDocumentController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public MedicalDocumentController(
        IMediator mediator,
        IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetDocuments(
        [FromQuery] GetDocumentsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return SuccessResponse(result);
    }

    [HttpGet("{documentId:guid}")]
    public async Task<IActionResult> GetDocumentById(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetDocumentByIdQuery(documentId),
            cancellationToken);
        return SuccessResponse(result);
    }
}
