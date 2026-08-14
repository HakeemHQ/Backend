using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Features.Doctor.MedicalCvs.Commands;
using Hakeem.Application.Features.Doctor.MedicalCvs.DTOs;
using Hakeem.Application.Features.Doctor.MedicalCvs.Queries;
using Hakeem.Application.Resources;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Forms;
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

            return Ok(result);
        }

        [HttpGet]
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
        public async Task<IActionResult> Get(Guid patientId,CancellationToken cancellationToken)
        {
            var response = await _mediator.Send(new GetPatientMedicalCvsQuery(patientId),cancellationToken);
            return Ok(response);
        }
    }
}
