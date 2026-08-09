using Hakeem.Domain.DomainEvents.Outbox;

namespace Hakeem.Application.Abstractions;

public interface IOutboxEventFailureHandler<in TEvent>
    where TEvent : OutboxEventBase
{
    Task HandleFailureAsync(
        TEvent @event,
        CancellationToken cancellationToken);
}
