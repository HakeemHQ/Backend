using Hakeem.Domain.DomainEvents.Outbox;

namespace Hakeem.Application.Abstractions;

/// <summary>
/// Handler interface for outbox-dispatched events.
/// Each event type has its own handler, resolved from DI by the background processor.
/// This mirrors MediatR's INotificationHandler pattern but is completely independent.
/// </summary>
public interface IOutboxEventHandler<in TEvent> where TEvent : OutboxEventBase
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken);
}
