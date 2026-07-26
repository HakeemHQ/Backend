using MediatR;

namespace Hakeem.Application.Services.Auth.Refresh;

public sealed class RefreshCommand : IRequest<RefreshResult>
{
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed record RefreshResult(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt);
