using FluentValidation;
using Hakeem.Application.Resources;
using Microsoft.Extensions.Localization;

namespace Hakeem.Application.Features.PatientAccessSessions.Commands.CreatePatientAccessSession;

public sealed class CreatePatientAccessSessionCommandValidator
    : AbstractValidator<CreatePatientAccessSessionCommand>
{
    public CreatePatientAccessSessionCommandValidator(
        IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(command => command.PatientCode)
            .NotEmpty()
            .WithMessage(localizer["Validation.Required"].Value)
            .MaximumLength(8)
            .WithMessage(localizer["PatientIdentity.InvalidPatientCode"].Value);

        RuleFor(command => command.OneTimeCode)
            .NotEmpty()
            .WithMessage(localizer["Validation.Required"].Value)
            .Matches("^[0-9]{6}$")
            .WithMessage(localizer["PatientAccess.InvalidCode"].Value);
    }
}
