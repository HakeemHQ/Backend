using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Interfaces.Services.Auth;

public interface ITokenService : IScoped
{
    IssuedTokenPair CreateTokenPair(User user);
    IssuedPasswordResetToken CreatePasswordResetToken();
    string HashRefreshToken(string refreshToken);
    string HashPasswordResetToken(string passwordResetToken);
}

public sealed record IssuedTokenPair(
    string AccessToken,
    string RefreshToken,
    string JwtId,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt);

public sealed record IssuedPasswordResetToken(
    string Token,
    string TokenHash,
    DateTime ExpiresAt);
