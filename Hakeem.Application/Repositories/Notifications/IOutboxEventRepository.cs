using Hakeem.Domain.DomainEvents.Outbox;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Repositories.Notifications;

/// <summary>
/// Repository for adding outbox event rows to the EF change tracker.
/// The actual persistence happens when the caller's UnitOfWork calls SaveChanges,
/// ensuring transactional consistency between business data and outbox events.
/// </summary>
public interface IOutboxEventRepository : IScoped
{
    /// <summary>
    /// Serializes the event and adds an OutboxEvent row to the EF change tracker.
    /// Does NOT call SaveChanges — the caller's UnitOfWork handles that.
    /// </summary>
    void Add<TEvent>(TEvent @event, string? idempotencyKey = null) where TEvent : OutboxEventBase;

    /// <summary>
    /// Checks whether an event with the given idempotency key already exists.
    /// </summary>
    Task<bool> ExistsByIdempotencyKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves pending outbox events independently of the caller's UnitOfWork.
    /// Use this when the outbox lives in a different DbContext than the business data
    /// (e.g., RequestService uses ApplicationDbContext while outbox is in ApplicationDbContext).
    /// </summary>
    Task SaveAsync(CancellationToken cancellationToken = default);
}
