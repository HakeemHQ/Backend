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

    public Task<RefreshToken?> GetByJwtIdAsync(
        string jwtId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.RefreshTokens.SingleOrDefaultAsync(
            refreshToken =>
                refreshToken.JwtId == jwtId &&
                refreshToken.UserId == userId,
            cancellationToken);
    }

    public Task<bool> IsSessionActiveAsync(
        string jwtId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.RefreshTokens.AnyAsync(
            refreshToken =>
                refreshToken.JwtId == jwtId &&
                refreshToken.UserId == userId &&
                !refreshToken.IsUsed &&
                !refreshToken.IsRevoked,
            cancellationToken);
    }

    public void Add(RefreshToken refreshToken)
    {
        dbContext.RefreshTokens.Add(refreshToken);
    }
}
