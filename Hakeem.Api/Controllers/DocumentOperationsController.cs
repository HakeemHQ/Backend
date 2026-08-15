using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Constants;
using Hakeem.Application.Features.MedicalDocuments.Commands.DeleteDocument;
using Hakeem.Application.Resources;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Hakeem.Api.Controllers;

[ApiController]
[Route("documents")]
public sealed class DocumentOperationsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public DocumentOperationsController(
        IMediator mediator,
        IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _mediator = mediator;
    }

    [HttpGet("{documentId:guid}/content")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContent(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new Hakeem.Application.Features.MedicalDocuments.Queries.GetDocumentContent.GetDocumentContentQuery(
                documentId),
            cancellationToken);

        return File(result.ContentStream, result.ContentType, result.FileName);
    }

    [HttpDelete("{documentId:guid}")]
    [Authorize(Roles = nameof(Hakeem.Domain.Enums.Identity.ApplicationRole.Doctor))]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteDocumentCommand(documentId), cancellationToken);
        return SuccessResponse(ErrorCodes.DocumentDeleted);
    }
}
