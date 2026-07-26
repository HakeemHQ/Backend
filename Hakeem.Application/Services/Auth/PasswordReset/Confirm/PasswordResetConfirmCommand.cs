using MediatR;

namespace Hakeem.Application.Services.Auth.PasswordReset.Confirm;

public sealed class PasswordResetConfirmCommand : IRequest<Unit>
{
    public string ResetToken { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
