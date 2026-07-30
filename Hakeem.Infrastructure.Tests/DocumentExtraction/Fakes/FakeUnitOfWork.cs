using Hakeem.Domain.Interfaces;

namespace Hakeem.Infrastructure.Tests.DocumentExtraction.Fakes;

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCallCount { get; private set; }
    public int BeginTransactionCallCount { get; private set; }
    public int CommitTransactionCallCount { get; private set; }
    public int RollbackTransactionCallCount { get; private set; }

    public Task<int> SaveChanges()
    {
        SaveChangesCallCount++;
        return Task.FromResult(1);
    }

    public Task<int> SaveChanges(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SaveChangesCallCount++;
        return Task.FromResult(1);
    }

    public Task BeginTransactionAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        BeginTransactionCallCount++;
        return Task.CompletedTask;
    }

    public Task CommitTransactionAsync()
    {
        CommitTransactionCallCount++;
        return Task.CompletedTask;
    }

    public Task RollBackTransactionAsync()
    {
        RollbackTransactionCallCount++;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }
}
