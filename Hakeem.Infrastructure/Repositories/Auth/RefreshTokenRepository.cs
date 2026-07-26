using Hakeem.Application.Repositories.Auth;
using Hakeem.Domain.Entities;
using Hakeem.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Hakeem.Infrastructure.Repositories.Auth;

public sealed class RefreshTokenRepository(ApplicationDbContext dbContext)
    : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByTokenAsync(
        string hashedToken,
        CancellationToken cancellationToken)
    {
        return dbContext.RefreshTokens
            .Include(refreshToken => refreshToken.User)
            .SingleOrDefaultAsync(
                refreshToken => refreshToken.Token == hashedToken,
                cancellationToken);
    }

    public void Add(RefreshToken refreshToken)
    {
        dbContext.RefreshTokens.Add(refreshToken);
    }
}
