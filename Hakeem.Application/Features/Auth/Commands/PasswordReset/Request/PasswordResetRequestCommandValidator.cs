using FluentValidation;
using Hakeem.Application.Resources;
using Microsoft.Extensions.Localization;

namespace Hakeem.Application.Features.Auth.Commands.PasswordReset.Request;

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
