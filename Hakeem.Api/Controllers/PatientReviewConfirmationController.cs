using FluentValidation;
using Hakeem.Application.Features.PatientReviewAndConfirmation.Commands;
using Hakeem.Application.Resources;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Hakeem.Api.Controllers
{
    [Route("extracted-items")]
    [Authorize(Roles = "Patient")]
    public sealed class PatientReviewConfirmationController:ApiControllerBase
    {
        private readonly IMediator _mediator;
        public PatientReviewConfirmationController(IMediator mediator, IStringLocalizer<SharedResource> localizer):base(localizer)
        {
            _mediator = mediator;
        }

        [HttpPut("{extractedItemId:guid}/review")]
        public async Task<IActionResult> ReviewItem(Guid extractedItemId,[FromBody] PatientReviewConfirmationCommand command,
        CancellationToken cancellationToken)
        {
            command.ExtractedItemId = extractedItemId;

            var result = await _mediator.Send(command, cancellationToken);

            return SuccessResponse(result);
        }
    }
}
