using FluentValidation;

namespace Hakeem.Application.Services.Auth;

public sealed class PasswordResetRequestCommandValidator : AbstractValidator<PasswordResetRequestCommand>
{
    public PasswordResetRequestCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Invalid email format.");
    }
}
