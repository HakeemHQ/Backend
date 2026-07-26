using MediatR;

namespace Hakeem.Application.Services.Auth.PasswordReset.Request;

public sealed class PasswordResetRequestCommand : IRequest<Unit>
{
    public string Email { get; set; } = string.Empty;
}
