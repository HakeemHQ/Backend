using System.Text.Json;

using Hakeem.Application.Repositories.Notifications;
using Hakeem.Domain.DomainEvents.Outbox;
using Hakeem.Domain.Entities.NotificationsEntites;
using Hakeem.Domain.Enums;
using Hakeem.Domain.Enums.Notifications;
using Hakeem.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Hakeem.Infrastructure.Repositories.Notifications;

public class OutboxEventRepository : IOutboxEventRepository
{
    private readonly ApplicationDbContext _dbContext;

    public OutboxEventRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add<TEvent>(TEvent @event, string? idempotencyKey = null) where TEvent : OutboxEventBase
    {
        var outboxEvent = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = typeof(TEvent).Name,
            Payload = JsonSerializer.Serialize(@event),
            Status = OutboxEventStatus.Pending,
            RetryCount = 0,
            CreatedAt = DateTime.UtcNow,
            IdempotencyKey = idempotencyKey
        };

        _dbContext.OutboxEvents.Add(outboxEvent);
    }

    public async Task<bool> ExistsByIdempotencyKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _dbContext.OutboxEvents
            .AnyAsync(e => e.IdempotencyKey == key, cancellationToken);
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
