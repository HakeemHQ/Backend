using Hakeem.Domain.DomainEvents.Outbox;

namespace Hakeem.Application.Abstractions;


public interface IOutboxEventHandler<in TEvent> where TEvent : OutboxEventBase
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken);
}
