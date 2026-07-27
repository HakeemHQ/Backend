using Hakeem.Application.Repositories.Auth;
using Hakeem.Domain.Entities;
using Hakeem.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Hakeem.Infrastructure.Repositories.Auth;

public sealed class PasswordResetTokenRepository(ApplicationDbContext dbContext)
    : IPasswordResetTokenRepository
{
    public Task<PasswordResetToken?> GetByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        return dbContext.PasswordResetTokens.SingleOrDefaultAsync(
            token => token.TokenHash == tokenHash,
            cancellationToken);
    }

    public void Add(PasswordResetToken passwordResetToken)
    {
        dbContext.PasswordResetTokens.Add(passwordResetToken);
    }
}
