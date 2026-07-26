using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Services.Auth.Tokens;

public interface ITokenService : IScoped
{
    IssuedTokenPair CreateTokenPair(User user);
    string HashRefreshToken(string refreshToken);
}

public sealed record IssuedTokenPair(
    string AccessToken,
    string RefreshToken,
    string JwtId,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt);
