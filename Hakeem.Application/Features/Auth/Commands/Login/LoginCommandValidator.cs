using FluentValidation;
using Hakeem.Application.Resources;
using Microsoft.Extensions.Localization;

namespace Hakeem.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator(IStringLocalizer<SharedResource> localizer)
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
            .WithMessage(localizer["Validation.Required"].Value);
    }
}
