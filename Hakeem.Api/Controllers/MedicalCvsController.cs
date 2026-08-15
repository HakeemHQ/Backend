using Hakeem.Api.Authorization;
using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Constants;
using Hakeem.Application.Features.MedicalCvs.Commands.GenerateFullMedicalCv;
using Hakeem.Application.Features.MedicalCvs.Queries.GetMedicalCvById;
using Hakeem.Application.Features.MedicalCvs.Queries.GetMedicalCvPdf;
using Hakeem.Application.Features.MedicalCvs.Queries.GetMedicalCvs;
using Hakeem.Application.Resources;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Hakeem.Api.Controllers;

[Route("medical-cvs")]
[Authorize]
public sealed class MedicalCvsController(
    IMediator mediator,
    IStringLocalizer<SharedResource> localizer)
    : ApiControllerBase(localizer)
{
    [HttpGet]
    [ProducesResponseType(
        typeof(GenericResponseModel<GetMedicalCvsResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMedicalCvs(
        [FromQuery] GetMedicalCvsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        return SuccessResponse(result);
    }

    [HttpGet("{medicalCvId:guid}")]
    [ProducesResponseType(
        typeof(GenericResponseModel<GetMedicalCvByIdResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMedicalCvById(
        Guid medicalCvId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetMedicalCvByIdQuery(medicalCvId),
            cancellationToken);

        return SuccessResponse(result);
    }

    [HttpPost]
    [Authorize(Policy = VerifiedPatientPolicy.Name)]
    [ProducesResponseType(
        typeof(GenericResponseModel<GenerateFullMedicalCvResponse>),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GenerateFullMedicalCv(
        [FromBody] GenerateFullMedicalCvCommand request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(request, cancellationToken);
        return CreatedResponse(result, ErrorCodes.MedicalCvQueued);
    }

}
