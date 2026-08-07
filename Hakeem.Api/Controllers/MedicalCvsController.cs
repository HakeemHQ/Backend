using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Constants;
using Hakeem.Application.Features.MedicalCvs.Commands.GenerateFullMedicalCv;
using Hakeem.Application.Features.MedicalCvs.Commands.CreateMedicalCvPreviewLink;
using Hakeem.Application.Features.MedicalCvs.Queries.GetMedicalCvPdf;
using Hakeem.Application.Features.MedicalCvs.Queries.GetMedicalCvPreview;
using Hakeem.Application.Resources;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Hakeem.Api.Controllers;

[Route("medical-cvs")]
[Authorize(Roles = "Patient")]
public sealed class MedicalCvsController(
    IMediator mediator,
    IStringLocalizer<SharedResource> localizer)
    : ApiControllerBase(localizer)
{
    [HttpPost]
    [ProducesResponseType(
        typeof(GenericResponseModel<GenerateFullMedicalCvResponse>),
        StatusCodes.Status202Accepted)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GenerateFullMedicalCv(
        [FromBody] GenerateFullMedicalCvCommand request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(request, cancellationToken);
        return AcceptedResponse(result, ErrorCodes.MedicalCvQueued);
    }

    [HttpGet("{medicalCvId:guid}/versions/{medicalCvVersionId:guid}/pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetMedicalCvPdf(
        Guid medicalCvId,
        Guid medicalCvVersionId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetMedicalCvPdfQuery(medicalCvId, medicalCvVersionId),
            cancellationToken);

        return File(
            result.Content,
            "application/pdf",
            enableRangeProcessing: true);
    }

    [HttpPost("{medicalCvId:guid}/versions/{medicalCvVersionId:guid}/preview-link")]
    [ProducesResponseType(
        typeof(GenericResponseModel<CreateMedicalCvPreviewLinkResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateMedicalCvPreviewLink(
        Guid medicalCvId,
        Guid medicalCvVersionId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateMedicalCvPreviewLinkCommand(
                medicalCvId,
                medicalCvVersionId),
            cancellationToken);

        return SuccessResponse(result);
    }

    [HttpGet("{medicalCvId:guid}/versions/{medicalCvVersionId:guid}/preview")]
    [AllowAnonymous]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> PreviewMedicalCvPdf(
        Guid medicalCvId,
        Guid medicalCvVersionId,
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetMedicalCvPreviewQuery(
                medicalCvId,
                medicalCvVersionId,
                token),
            cancellationToken);

        return File(
            result.Content,
            "application/pdf",
            enableRangeProcessing: true);
    }
}
