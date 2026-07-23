using MediatR;
namespace Hakeem.Domain.Entities;

public abstract class DomainEvent : INotification
{
    public DateTime OccurredOn { get; protected set; } = DateTime.UtcNow;
}