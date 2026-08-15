using Hakeem.Application.Features.Admin.Users.GetUsers.DTOs;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Identity;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Repositories.Users;

public interface IUserRepository : IScoped
{
    void Add(User user);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<(IEnumerable<AdminUserDto> Items, int TotalCount)> GetUsersAsync(
   string? search,
   ApplicationRole? userType,
   AccountStatus? status,
   int pageNumber,
   int pageSize,
   CancellationToken cancellationToken);

}
