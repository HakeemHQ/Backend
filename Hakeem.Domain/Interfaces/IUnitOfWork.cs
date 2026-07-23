using System.Data;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Domain.Interfaces;

public interface IUnitOfWork : IDisposable, IScoped
{
    Task<int> SaveChanges();
    Task<int> SaveChanges(CancellationToken cancellationToken);

    Task BeginTransactionAsync(CancellationToken cancellationToken);

    Task CommitTransactionAsync();

    Task RollBackTransactionAsync();
}
