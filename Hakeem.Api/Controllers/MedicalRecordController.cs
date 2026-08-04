using Hakeem.Application.Features.MedicalRecords.Queries.GetMedicalRecordById;
using Hakeem.Application.Features.MedicalRecords.Queries.GetMedicalRecords;
using Hakeem.Application.Resources;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Hakeem.Api.Controllers
{
    [Route("medical-records")]
    [Authorize(Roles = "Patient")]
    public class MedicalRecordController : ApiControllerBase
    {
        private readonly IMediator _mediator;
        public MedicalRecordController(IMediator mediator, IStringLocalizer<SharedResource> localizer) : base(localizer)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetMedicalRecords([FromQuery] GetMedicalRecordsQuery query,CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return SuccessResponse(result);
        }

        [HttpGet("{medicalRecordId:guid}")]
        public async Task<IActionResult> GetMedicalRecordById(Guid medicalRecordId,CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMedicalRecordByIdQuery(medicalRecordId),cancellationToken);
            return SuccessResponse(result);
        }

    }

}

