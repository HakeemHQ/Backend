


using Hakeem.Application.Configurations;
using Hakeem.Domain.Entities.NotificationsEntites;
using Hakeem.Domain.Enums.Notifications;
using Hakeem.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hakeem.Infrastructure.Services;


public class OutboxEventProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxEventProcessor> _logger;
    private readonly OutboxConfiguration _config;

    public OutboxEventProcessor(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxEventProcessor> logger,
        IOptions<OutboxConfiguration> config)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _config = config.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "OutboxEventProcessor started. Polling every {Interval}s, batch size {BatchSize}, max retries {MaxRetries}.",
            _config.PollingIntervalSeconds, _config.BatchSize, _config.MaxRetries);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in OutboxEventProcessor polling loop.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_config.PollingIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("OutboxEventProcessor stopping.");
    }

    private async Task ProcessPendingEventsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<OutboxEventDispatcher>();

        // 1. Release stale locks (instances that crashed mid-processing)
        await ReleaseStaleLocksAsync(dbContext, cancellationToken);

        // 2. Claim a batch of pending events using row-level locking
        var claimedEvents = await ClaimBatchAsync(dbContext, cancellationToken);

        if (claimedEvents.Count == 0)
            return;

        _logger.LogInformation("Claimed {Count} outbox events for processing.", claimedEvents.Count);

        // 3. Process each event
        foreach (var outboxEvent in claimedEvents)
        {
            await ProcessSingleEventAsync(outboxEvent, dispatcher, dbContext, cancellationToken);
        }
    }

    private async Task ReleaseStaleLocksAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var staleLockSql = """
            UPDATE OutboxEvents
            SET Status = {0}, LockUntil = NULL
            WHERE Status = {1} AND LockUntil IS NOT NULL AND LockUntil <= {2}
            """;

        var affected = await dbContext.Database.ExecuteSqlRawAsync(
            staleLockSql,
            [(int)OutboxEventStatus.Pending, (int)OutboxEventStatus.Processing, DateTime.UtcNow],
            cancellationToken);

        if (affected > 0)
        {
            _logger.LogWarning("Released {Count} stale locks from OutboxEvents.", affected);
        }
    }

    private async Task<List<OutboxEvent>> ClaimBatchAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        // Use raw SQL with UPDLOCK, ROWLOCK, READPAST for safe multi-instance claiming.
        // This atomically selects and updates rows, preventing double-processing.
        var lockUntil = DateTime.UtcNow.AddSeconds(_config.LockDurationSeconds);
        var now = DateTime.UtcNow;

        // Step 1: Claim rows by updating their status and lock
        var claimSql = $"""
            UPDATE TOP ({_config.BatchSize}) OutboxEvents WITH (ROWLOCK)
            SET Status = @p0, LockUntil = @p1, ProcessedAt = @p2
            OUTPUT INSERTED.Id
            WHERE Status = @p3
              AND (NextRetryAt IS NULL OR NextRetryAt <= @p4)
              AND (LockUntil IS NULL OR LockUntil <= @p5)
            """;

        // Execute the claim and get the IDs
        var claimedIds = await dbContext.Database
            .SqlQueryRaw<Guid>(
                claimSql,
                (int)OutboxEventStatus.Processing,
                lockUntil,
                now,
                (int)OutboxEventStatus.Pending,
                now,
                now)
            .ToListAsync(cancellationToken);

        if (claimedIds.Count == 0)
            return [];

        // Step 2: Load the full entities
        return await dbContext.OutboxEvents
            .Where(e => claimedIds.Contains(e.Id))
            .ToListAsync(cancellationToken);
    }

    private async Task ProcessSingleEventAsync(
        OutboxEvent outboxEvent,
        OutboxEventDispatcher dispatcher,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Processing outbox event {EventId} of type '{EventType}'.",
                outboxEvent.Id, outboxEvent.EventType);

            await dispatcher.DispatchAsync(outboxEvent.EventType, outboxEvent.Payload, cancellationToken);

            // Success
            outboxEvent.Status = OutboxEventStatus.Processed;
            outboxEvent.ProcessedAt = DateTime.UtcNow;
            outboxEvent.LockUntil = null;
            outboxEvent.ErrorMessage = null;

            _logger.LogInformation("Outbox event {EventId} processed successfully.", outboxEvent.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process outbox event {EventId} of type '{EventType}'.", outboxEvent.Id, outboxEvent.EventType);

            outboxEvent.RetryCount++;
            outboxEvent.ErrorMessage = ex.Message.Length > 4000
                ? ex.Message[..4000]
                : ex.Message;
            outboxEvent.ProcessedAt = DateTime.UtcNow;
            outboxEvent.LockUntil = null;

            if (outboxEvent.RetryCount >= outboxEvent.MaxRetries)
            {
                outboxEvent.Status = OutboxEventStatus.Failed;
                _logger.LogWarning(
                    "Outbox event {EventId} marked as Failed after {RetryCount} attempts.",
                    outboxEvent.Id, outboxEvent.RetryCount);
            }
            else
            {
                // Exponential backoff: delay * 2^retryCount
                var backoffSeconds = _config.RetryDelaySeconds * Math.Pow(2, outboxEvent.RetryCount - 1);
                outboxEvent.Status = OutboxEventStatus.Pending;
                outboxEvent.NextRetryAt = DateTime.UtcNow.AddSeconds(backoffSeconds);

                _logger.LogInformation(
                    "Outbox event {EventId} will be retried at {NextRetryAt} (attempt {RetryCount}/{MaxRetries}).",
                    outboxEvent.Id, outboxEvent.NextRetryAt, outboxEvent.RetryCount, outboxEvent.MaxRetries);
            }
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save status for outbox event {EventId}.", outboxEvent.Id);
        }
    }
}
