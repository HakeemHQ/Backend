using FluentValidation;

namespace Hakeem.Application.Services.Auth;

public sealed class PasswordResetConfirmCommandValidator : AbstractValidator<PasswordResetConfirmCommand>
{
    public PasswordResetConfirmCommandValidator()
    {
        RuleFor(x => x.ResetToken)
            .NotEmpty()
            .WithMessage("Reset token is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required.")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Z]")
            .WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]")
            .WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]")
            .WithMessage("Password must contain at least one number.")
            .Matches("[^A-Za-z0-9]")
            .WithMessage("Password must contain at least one special character.");
    }
}
