using FluentValidation;
using Hakeem.Application.Constants;
using Hakeem.Application.Resources;
using Microsoft.Extensions.Localization;

namespace Hakeem.Application.Features.Doctor.MedicalCvs.Commands;

public sealed class GenerateDoctorMedicalCvCommandValidator
    : AbstractValidator<GenerateDoctorMedicalCvCommand>
{
    public GenerateDoctorMedicalCvCommandValidator(
        IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(command => command.PatientId)
            .NotEmpty();

        RuleFor(command => command.Title)
            .NotEmpty()
            .WithMessage(localizer[ErrorCodes.MedicalCvTitleRequired].Value)
            .WithErrorCode(ErrorCodes.MedicalCvTitleRequired)
            .MaximumLength(200)
            .WithMessage(localizer[ErrorCodes.MedicalCvTitleTooLong].Value)
            .WithErrorCode(ErrorCodes.MedicalCvTitleTooLong);
    }
}
