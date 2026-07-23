using Hakeem.Domain.DomainEvents.Outbox;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Repositories.Notifications;


public interface IOutboxEventRepository : IScoped
{

    void Add<TEvent>(TEvent @event, string? idempotencyKey = null) where TEvent : OutboxEventBase;

    Task<bool> ExistsByIdempotencyKeyAsync(string key, CancellationToken cancellationToken = default);


    Task SaveAsync(CancellationToken cancellationToken = default);
}
