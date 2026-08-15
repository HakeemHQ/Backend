using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Repositories.Auth;

public interface IRefreshTokenRepository : IScoped
{
    Task<RefreshToken?> GetByTokenAsync(
        string hashedToken,
        CancellationToken cancellationToken);

    Task<RefreshToken?> GetByJwtIdAsync(
        string jwtId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<bool> IsSessionActiveAsync(
        string jwtId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<int> RevokeAllActiveForUserAsync(
        Guid userId,
        DateTime revokedAt,
        CancellationToken cancellationToken);

    void Add(RefreshToken refreshToken);
}
