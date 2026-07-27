using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Hakeem.Application.Configurations;
using Hakeem.Application.Interfaces.Services.Auth;
using Hakeem.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Hakeem.Infrastructure.Services.Auth.Tokens;

public sealed class TokenService(IOptions<JwtConfiguration> options) : ITokenService
{
    private static readonly TimeSpan PasswordResetTokenLifetime = TimeSpan.FromMinutes(15);
    private readonly JwtConfiguration _configuration = options.Value;

    public IssuedTokenPair CreateTokenPair(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var issuedAt = DateTime.UtcNow;
        var accessTokenExpiresAt = issuedAt.AddMinutes(_configuration.TokenExpirationMinutes);
        var refreshTokenExpiresAt = issuedAt.AddDays(_configuration.RefreshTokenExpirationDays);
        var jwtId = Guid.NewGuid().ToString("N");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()),
            new Claim(ClaimTypes.Role, user.UserType),
            new Claim("userId", user.Id.ToString()),
            new Claim("userType", user.UserType),
            new Claim("status", user.Status),
            new Claim(JwtRegisteredClaimNames.Jti, jwtId)
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.Key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _configuration.Issuer,
            audience: _configuration.Audience,
            claims: claims,
            notBefore: issuedAt,
            expires: accessTokenExpiresAt,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));

        return new IssuedTokenPair(
            accessToken,
            refreshToken,
            jwtId,
            accessTokenExpiresAt,
            refreshTokenExpiresAt);
    }

    public IssuedPasswordResetToken CreatePasswordResetToken()
    {
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));

        return new IssuedPasswordResetToken(
            token,
            HashPasswordResetToken(token),
            DateTime.UtcNow.Add(PasswordResetTokenLifetime));
    }

    public string HashRefreshToken(string refreshToken)
    {
        return HashToken(refreshToken);
    }

    public string HashPasswordResetToken(string passwordResetToken)
    {
        return HashToken(passwordResetToken);
    }

    private static string HashToken(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var hashBytes = SHA256.HashData(tokenBytes);

        return Convert.ToHexString(hashBytes);
    }
}
