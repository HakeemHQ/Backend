using MediatR;

namespace Hakeem.Application.Features.Auth.Commands.PasswordReset.Request;

public sealed class PasswordResetRequestCommand : IRequest<Unit>
{
    public string Email { get; set; } = string.Empty;
}
