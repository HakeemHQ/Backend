using FluentValidation;
using Hakeem.Application.Resources;
using Microsoft.Extensions.Localization;

namespace Hakeem.Application.Features.PatientIdentities.Commands.VerifyPatientIdentity;

public sealed class VerifyPatientIdentityCommandValidator
    : AbstractValidator<VerifyPatientIdentityCommand>
{
    public VerifyPatientIdentityCommandValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(command => command.PatientCode)
            .NotEmpty()
            .WithMessage(localizer["Validation.Required"].Value)
            .MaximumLength(8)
            .WithMessage(localizer["PatientIdentity.InvalidPatientCode"].Value);

        RuleFor(command => command.NationalId)
            .NotEmpty()
            .WithMessage(localizer["Validation.Required"].Value)
            .Matches("^[0-9]{14}$")
            .WithMessage(localizer["Validation.InvalidNationalId"].Value);
    }
}
