using MediatR;

namespace Hakeem.Application.Services.Auth;

public sealed class PasswordResetRequestCommand : IRequest<Unit>
{
    public string Email { get; set; } = string.Empty;
}
