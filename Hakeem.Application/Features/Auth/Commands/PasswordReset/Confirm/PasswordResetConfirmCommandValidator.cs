using FluentValidation;
using Hakeem.Application.Resources;
using Microsoft.Extensions.Localization;

namespace Hakeem.Application.Features.Auth.Commands.PasswordReset.Confirm;

public sealed class PasswordResetConfirmCommandValidator : AbstractValidator<PasswordResetConfirmCommand>
{
    public PasswordResetConfirmCommandValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.ResetToken)
            .NotEmpty()
            .WithMessage(localizer["Validation.Required"].Value);

        RuleFor(x => x.NewPassword)
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
    }
}
