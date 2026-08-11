using FluentValidation;
using Hakeem.Application.Resources;
using Microsoft.Extensions.Localization;

namespace Hakeem.Application.Features.PatientAccessRequests.Commands.CreatePatientAccessRequest;

public sealed class CreatePatientAccessRequestCommandValidator
    : AbstractValidator<CreatePatientAccessRequestCommand>
{
    public CreatePatientAccessRequestCommandValidator(
        IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(command => command.PatientCode)
            .NotEmpty()
            .WithMessage(localizer["Validation.Required"].Value)
            .MaximumLength(8)
            .WithMessage(localizer["PatientIdentity.InvalidPatientCode"].Value);
    }
}
