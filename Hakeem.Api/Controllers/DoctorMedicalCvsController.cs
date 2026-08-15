using Hakeem.Api.Authorization;
using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Constants;
using Hakeem.Application.Features.Doctor.MedicalCvs.Commands;
using Hakeem.Application.Features.Doctor.MedicalCvs.DTOs;
using Hakeem.Application.Features.Doctor.MedicalCvs.Queries;
using Hakeem.Application.Resources;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Hakeem.Api.Controllers
{
    [ApiController]
    [Route("doctor/patients/{patientId:guid}/medical-cvs")]
    [Authorize]
    public class DoctorMedicalCvsController : ApiControllerBase
    {
        private readonly IMediator _mediator;
        public DoctorMedicalCvsController(IMediator mediator,IStringLocalizer<SharedResource> localizer) : base(localizer)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [Authorize(Policy = DoctorPatientAccessPolicy.Name)]
        [ProducesResponseType(
            typeof(GenericResponseModel<GenerateDoctorMedicalCvResponse>),
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
        public async Task<IActionResult> Generate(
        Guid patientId,
        [FromBody] GenerateDoctorMedicalCvRequest request,
        CancellationToken cancellationToken)
        {
            var command = new GenerateDoctorMedicalCvCommand(
                patientId,
                request.Title);

            var result = await _mediator.Send(
                command,
                cancellationToken);

            return CreatedResponse(result, ErrorCodes.MedicalCvQueued);
        }

        [HttpGet]
        [Authorize(Policy = DoctorPatientAccessPolicy.Name)]
        [ProducesResponseType(
    typeof(GenericResponseModel<IReadOnlyList<DoctorMedicalCvResponse>>),
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
        public async Task<ActionResult<IReadOnlyList<DoctorMedicalCvListItem>>> 
            GetPatientMedicalCvs(Guid patientId, 
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default) 
        { 
            var result = await _mediator.Send(
                new GetPatientMedicalCvsQuery
                (patientId, page, pageSize), cancellationToken); return Ok(new { items = result }); }
    }
}
