using Hakeem.Application.Repositories.Users;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces.ServiceLifetime;
using Hakeem.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Hakeem.Infrastructure.Repositories.Users;

public class UserRepository : IUserRepository, IScoped
{
    private readonly ApplicationDbContext _dbContext;

    public UserRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(User user)
    {
        _dbContext.Users.Add(user);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Email.ToLower() == email.ToLower(), cancellationToken);
    }

    public async Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }
}
