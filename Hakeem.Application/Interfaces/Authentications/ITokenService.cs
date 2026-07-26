using Hakeem.Domain.Entities;

namespace Hakeem.Application.Interfaces.Authentications
{
    public interface ITokenService
    {
        Task<string> CreateTokenAsync(User user);
        public string GenerateRefreshToken();
    }
}
