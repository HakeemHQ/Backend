using FluentValidation;
using Hakeem.Application.Resources;
using Microsoft.Extensions.Localization;

namespace Hakeem.Application.Services.Auth.Registration;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage(localizer["Validation.Required"].Value)
            .MaximumLength(256)
            .WithMessage(localizer["Validation.EmailTooLong"].Value)
            .EmailAddress()
            .WithMessage(localizer["Validation.InvalidEmail"].Value);

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage(localizer["Validation.Required"].Value)
            .MinimumLength(8)
            .WithMessage(localizer["Validation.PasswordTooShort"].Value)
            .Matches("[A-Z]")
            .WithMessage(localizer["Validation.PasswordMissingUppercase"].Value)
            .Matches("[a-z]")
            .WithMessage(localizer["Validation.PasswordMissingLowercase"].Value)
            .Matches("[0-9]")
            .WithMessage(localizer["Validation.PasswordMissingDigit"].Value)
            .Matches("[^A-Za-z0-9]")
            .WithMessage(localizer["Validation.PasswordMissingSpecial"].Value);

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage(localizer["Validation.Required"].Value)
            .MaximumLength(100)
            .WithMessage(localizer["Validation.FirstNameTooLong"].Value);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage(localizer["Validation.Required"].Value)
            .MaximumLength(100)
            .WithMessage(localizer["Validation.LastNameTooLong"].Value);

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .WithMessage(localizer["Validation.Required"].Value)
            .Matches(@"^\+?\d{8,15}$")
            .WithMessage(localizer["Validation.InvalidPhoneNumber"].Value);

        RuleFor(x => x.Gender)
            .NotEmpty()
            .WithMessage(localizer["Validation.Required"].Value)
            .Must(gender =>
                string.Equals(gender?.Trim(), "Male", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(gender?.Trim(), "Female", StringComparison.OrdinalIgnoreCase))
            .WithMessage(localizer["Validation.InvalidGender"].Value);

        RuleFor(x => x.BirthDate)
            .NotEmpty()
            .WithMessage(localizer["Validation.Required"].Value)
            .LessThan(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage(localizer["Validation.BirthDateInPast"].Value);
    }
}
