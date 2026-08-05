using Hakeem.Domain.DomainEvents;
using MediatR;

namespace Hakeem.Application.Abstractions;

public interface IDomainEventHandler<T> : INotificationHandler<T>
    where T : DomainEvent
{
}
