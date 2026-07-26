using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Repositories.Auth;

public interface IRefreshTokenRepository : IScoped
{
    void Add(RefreshToken refreshToken);
}
