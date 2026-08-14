using Hakeem.Application.Features.Admin.Users.GetUsers.DTOs;
using Hakeem.Application.Repositories.Users;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Identity;
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

    public async Task<(IEnumerable<AdminUserDto> Items, int TotalCount)> GetUsersAsync(
   string? search,
   AccountStatus? status,
   int pageNumber,
   int pageSize,
   CancellationToken cancellationToken)
    {
        var query = _dbContext.Users
            .AsNoTracking()
            .Where(x => x.Role == Domain.Enums.Identity.ApplicationRole.Patient)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.Email.Contains(search));
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.Email)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdminUserDto
            {
                UserId = x.Id,
                Email = x.Email,
                UserType = x.Role,
                Status = x.Status
            })
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
