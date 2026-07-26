using MediatR;

namespace Hakeem.Application.Features.Auth.Commands.Login;

public sealed class LoginCommand : IRequest<LoginResult>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed record LoginResult(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt,
    LoginUserResult User);

public sealed record LoginUserResult(
    Guid UserId,
    string Email,
    string UserType);
