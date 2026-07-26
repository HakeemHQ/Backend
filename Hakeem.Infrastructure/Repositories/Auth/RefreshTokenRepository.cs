using Hakeem.Application.Repositories.Auth;
using Hakeem.Domain.Entities;
using Hakeem.Infrastructure.Context;

namespace Hakeem.Infrastructure.Repositories.Auth;

public sealed class RefreshTokenRepository(ApplicationDbContext dbContext)
    : IRefreshTokenRepository
{
    public void Add(RefreshToken refreshToken)
    {
        dbContext.RefreshTokens.Add(refreshToken);
    }
}
