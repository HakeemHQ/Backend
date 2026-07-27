using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Repositories.Auth;

public interface IPasswordResetTokenRepository : IScoped
{
    Task<PasswordResetToken?> GetByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken);

    void Add(PasswordResetToken passwordResetToken);
}
