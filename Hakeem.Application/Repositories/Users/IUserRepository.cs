using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Repositories.Users;

public interface IUserRepository : IScoped
{
    void Add(User user);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);
}
