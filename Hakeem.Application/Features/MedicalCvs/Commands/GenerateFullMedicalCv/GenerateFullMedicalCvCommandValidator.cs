using FluentValidation;
using Hakeem.Application.Constants;
using Hakeem.Application.Resources;
using Microsoft.Extensions.Localization;

namespace Hakeem.Application.Features.MedicalCvs.Commands.GenerateFullMedicalCv;

public sealed class GenerateFullMedicalCvCommandValidator
    : AbstractValidator<GenerateFullMedicalCvCommand>
{
    public GenerateFullMedicalCvCommandValidator(
        IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(command => command.Title)
            .NotEmpty()
            .WithMessage(localizer[ErrorCodes.MedicalCvTitleRequired].Value)
            .WithErrorCode(ErrorCodes.MedicalCvTitleRequired)
            .MaximumLength(200)
            .WithMessage(localizer[ErrorCodes.MedicalCvTitleTooLong].Value)
            .WithErrorCode(ErrorCodes.MedicalCvTitleTooLong);
    }
}
