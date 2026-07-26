using FluentValidation;
using Hakeem.Application.Resources;
using Microsoft.Extensions.Localization;

namespace Hakeem.Application.Services.Auth;

public sealed class PasswordResetRequestCommandValidator : AbstractValidator<PasswordResetRequestCommand>
{
    public PasswordResetRequestCommandValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage(localizer["Validation.Required"].Value)
            .EmailAddress()
            .WithMessage(localizer["Validation.InvalidEmail"].Value);
    }
}
