using FluentValidation;
using Hakeem.Application.Constants;
using Hakeem.Application.Resources;
using Microsoft.Extensions.Localization;

namespace Hakeem.Application.Features.Admin.Doctors.Commands.AddDoctor
{
    public sealed class CreateDoctorCommandValidator
    : AbstractValidator<CreateDoctorCommand>
    {
        public CreateDoctorCommandValidator(
            IStringLocalizer<SharedResource> localizer)
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage(localizer[ErrorCodes.ValidationRequired].Value)
                .MaximumLength(256)
                .WithMessage(localizer[ErrorCodes.ValidationMaxLength].Value)
                .EmailAddress()
                .WithMessage(localizer[ErrorCodes.ValidationInvalidEmail].Value);

            RuleFor(x => x.FullName)
                .NotEmpty()
                .WithMessage(localizer[ErrorCodes.ValidationRequired].Value)
                .MaximumLength(200)
                .WithMessage(localizer[ErrorCodes.ValidationMaxLength].Value);

            RuleFor(x => x.Specialty)
                .NotEmpty()
                .WithMessage(localizer[ErrorCodes.ValidationRequired].Value)
                .MaximumLength(200)
                .WithMessage(localizer[ErrorCodes.ValidationMaxLength].Value);

            RuleFor(x => x.TemporaryPassword)
                .NotEmpty()
                .WithMessage(localizer[ErrorCodes.ValidationRequired].Value)
                .MinimumLength(8)
                .WithMessage(localizer[ErrorCodes.ValidationMinLength].Value)
                .MaximumLength(128)
                .WithMessage(localizer[ErrorCodes.ValidationMaxLength].Value)
                .Matches("[A-Z]")
                .WithMessage(localizer[ErrorCodes.ValidationInvalidFormat].Value)
                .Matches("[a-z]")
                .WithMessage(localizer[ErrorCodes.ValidationInvalidFormat].Value)
                .Matches("[0-9]")
                .WithMessage(localizer[ErrorCodes.ValidationInvalidFormat].Value)
                .Matches("[^A-Za-z0-9]")
                .WithMessage(localizer[ErrorCodes.ValidationInvalidFormat].Value);
        }
    }
}
