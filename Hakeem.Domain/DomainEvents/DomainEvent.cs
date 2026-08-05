using MediatR;
namespace Hakeem.Domain.DomainEvents;

public abstract class DomainEvent : INotification
{
    public DateTime OccurredOn { get; protected set; } = DateTime.UtcNow;
}