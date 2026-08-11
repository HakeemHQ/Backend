using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Hakeem.Application.Configurations;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Identity;
using Hakeem.Infrastructure.Services.Auth.Tokens;
using Microsoft.Extensions.Options;

namespace Hakeem.Infrastructure.Tests.Identity;

public sealed class TokenServiceTests
{
    [Theory]
    [InlineData(ApplicationRole.Patient)]
    [InlineData(ApplicationRole.Doctor)]
    [InlineData(ApplicationRole.Admin)]
    public void CreateTokenPair_UsesSharedClaimsForEveryRole(ApplicationRole role)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "identity@hakeem.test",
            FirstName = "Shared",
            LastName = "Identity",
            Role = role,
            Status = AccountStatus.Active
        };
        var service = new TokenService(Options.Create(new JwtConfiguration
        {
            Key = "a-development-test-key-that-is-at-least-32-characters",
            Issuer = "Hakeem.Tests",
            Audience = "Hakeem.Tests",
            TokenExpirationMinutes = 10,
            RefreshTokenExpirationDays = 7
        }));

        var pair = service.CreateTokenPair(user);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(pair.AccessToken);

        Assert.Equal(user.Id.ToString(), token.Subject);
        Assert.Equal(
            user.Id.ToString(),
            token.Claims.Single(claim => claim.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal(
            role.ToString(),
            token.Claims.Single(claim => claim.Type == ClaimTypes.Role).Value);
        Assert.Equal(
            role.ToString(),
            token.Claims.Single(claim => claim.Type == "userType").Value);
    }
}
