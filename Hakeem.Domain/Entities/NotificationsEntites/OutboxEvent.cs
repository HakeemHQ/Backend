using Hakeem.Domain.Enums.Notifications;

namespace Hakeem.Domain.Entities.NotificationsEntites;

/// <summary>
/// Persistent outbox table for deferred event processing.
/// Events are serialized into this table within the same transaction as the business operation,
/// then processed asynchronously by a background worker.
/// </summary>
public class OutboxEvent
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public OutboxEventStatus Status { get; set; } = OutboxEventStatus.Pending;
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public DateTime? LockUntil { get; set; }
    public string? IdempotencyKey { get; set; }
}
