using MediatR;

namespace Hakeem.Application.Features.Auth.Commands.PasswordReset.Confirm;

public sealed class PasswordResetConfirmCommand : IRequest<Unit>
{
    public string ResetToken { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
