using Hakeem.Api.Authorization;
using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Features.MedicalCvs.Commands.ApproveMedicalCvVersion;
using Hakeem.Application.Features.MedicalCvs.Commands.CreateMedicalCvPreviewLink;
using Hakeem.Application.Features.MedicalCvs.Queries.GetMedicalCvPdf;
using Hakeem.Application.Features.MedicalCvs.Queries.GetMedicalCvPreview;
using Hakeem.Application.Resources;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Hakeem.Api.Controllers;

[Route("medical-cv-versions")]
//[Authorize(Roles = "Patient")]
[Authorize]
public sealed class MedicalCvVersionsController(
    IMediator mediator,
    IStringLocalizer<SharedResource> localizer)
    : ApiControllerBase(localizer)
{
    [HttpPost("{versionId:guid}/approval")]
    [Authorize(Policy = PatientResourceAccessPolicy.DoctorOnly)]
    [ProducesResponseType(
        typeof(GenericResponseModel<ApproveMedicalCvVersionResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Approve(
        Guid versionId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ApproveMedicalCvVersionCommand(versionId),
            cancellationToken);

        return SuccessResponse(result);
    }

    [HttpGet("{versionId:guid}/pdf")]
    [Authorize(Policy = PatientResourceAccessPolicy.DoctorOrVerifiedPatient)]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetPdf(
        Guid versionId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetMedicalCvPdfQuery(versionId),
            cancellationToken);

        return File(result.Content, "application/pdf", enableRangeProcessing: true);
    }

    [HttpPost("{versionId:guid}/preview-link")]
    [ProducesResponseType(
        typeof(GenericResponseModel<CreateMedicalCvPreviewLinkResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreatePreviewLink(
        Guid versionId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateMedicalCvPreviewLinkCommand(versionId),
            cancellationToken);

        return SuccessResponse(result);
    }

    [HttpGet("{versionId:guid}/preview")]
    [AllowAnonymous]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(GenericResponseModel<object>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> PreviewPdf(
        Guid versionId,
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetMedicalCvPreviewQuery(versionId, token),
            cancellationToken);

        return File(result.Content, "application/pdf", enableRangeProcessing: true);
    }
}
