using Hakeem.Application.Configurations;
using Hakeem.Application.Interfaces.Authentications;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces.ServiceLifetime;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Hakeem.Infrastructure.Services.Authentications
{
    public class TokenService :ITokenService,IScoped
    {
        private readonly JwtConfiguration _jwtConfiguration;
        private readonly UserManager<User> _userManger;
        public TokenService(IOptions<JwtConfiguration>options ,UserManager<User>userManager)
        {
            _jwtConfiguration = options.Value;
            _userManger = userManager;
        }
        public async Task<string>CreateTokenAsync(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Name, user.FirstName!)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfiguration.Key));

            var credentials = new SigningCredentials(key,SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
             issuer: _jwtConfiguration.Issuer,
             audience: _jwtConfiguration.Audience,
             claims: claims,
             expires: DateTime.UtcNow.AddMinutes(
            _jwtConfiguration.TokenExpirationMinutes),
             signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            return Convert.ToBase64String(randomBytes);
        }
    }
}
