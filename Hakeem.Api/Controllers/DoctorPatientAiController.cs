using Hakeem.Api.Authorization;
using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Features.Doctor.MedicalIntelligence.Commands.Chat;
using Hakeem.Application.Resources;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Hakeem.Api.Controllers;

[ApiController]
[Route("doctor/patients/{patientId:guid}/ai")]
[Authorize(Policy = DoctorPatientAccessPolicy.Name)]
public sealed class DoctorPatientAiController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public DoctorPatientAiController(
        IMediator mediator,
        IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _mediator = mediator;
    }

    [HttpPost("questions")]
    [ProducesResponseType(
        typeof(GenericResponseModel<DoctorMedicalIntelligenceChatResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(GenericResponseModel<object>),
        StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AskQuestion(
        Guid patientId,
        [FromBody] DoctorAiQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(
            new DoctorMedicalIntelligenceChatCommand(
                patientId,
                request.Message),
            cancellationToken);

        return SuccessResponse(response);
    }
}

public sealed record DoctorAiQuestionRequest(string Message);
