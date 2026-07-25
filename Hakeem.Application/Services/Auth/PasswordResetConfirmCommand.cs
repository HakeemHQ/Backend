using MediatR;

namespace Hakeem.Application.Services.Auth;

public sealed class PasswordResetConfirmCommand : IRequest<Unit>
{
    public string ResetToken { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
