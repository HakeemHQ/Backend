using FluentValidation;
using Hakeem.Application.Resources;
using Microsoft.Extensions.Localization;

namespace Hakeem.Application.Features.PatientProfile.Commands.UpdateProfile;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage(localizer["Profile.ValidationFailed"].Value)
            .When(x => x.FullName is not null);

        RuleFor(x => x.BirthDate)
            .LessThan(DateTime.Today)
            .WithMessage(localizer["Validation.BirthDateInPast"].Value)
            .When(x => x.BirthDate.HasValue);

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage(localizer["Validation.Required"].Value)
            .MaximumLength(100)
            .WithMessage(localizer["Validation.FirstNameTooLong"].Value)
            .When(x => x.FirstName is not null);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage(localizer["Validation.Required"].Value)
            .MaximumLength(100)
            .WithMessage(localizer["Validation.LastNameTooLong"].Value)
            .When(x => x.LastName is not null);

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .WithMessage(localizer["Validation.Required"].Value)
            .Matches(@"^\+?\d{8,15}$")
            .WithMessage(localizer["Validation.InvalidPhoneNumber"].Value)
            .When(x => x.PhoneNumber is not null);

        RuleFor(x => x.Gender)
            .NotEmpty()
            .WithMessage(localizer["Validation.Required"].Value)
            .Must(gender =>
                string.Equals(gender?.Trim(), "Male", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(gender?.Trim(), "Female", StringComparison.OrdinalIgnoreCase))
            .WithMessage(localizer["Validation.InvalidGender"].Value)
            .When(x => x.Gender is not null);
    }
}
