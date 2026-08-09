using System.Text.Json;
using Hakeem.Application.Abstractions;
using Hakeem.Domain.DomainEvents.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hakeem.Infrastructure.Services;

public class OutboxEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxEventDispatcher> _logger;

    public OutboxEventDispatcher(IServiceProvider serviceProvider, ILogger<OutboxEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task DispatchAsync(string eventTypeName, string payload, CancellationToken cancellationToken)
    {
        var eventType = OutboxEventTypeRegistry.Resolve(eventTypeName);
        if (eventType is null)
        {
            _logger.LogWarning("No registered event type found for '{EventType}'. Skipping.", eventTypeName);
            return;
        }

        var @event = JsonSerializer.Deserialize(payload, eventType) as OutboxEventBase;
        if (@event is null)
        {
            throw new InvalidOperationException($"Failed to deserialize payload for event type '{eventTypeName}'.");
        }

        var handlerType = typeof(IOutboxEventHandler<>).MakeGenericType(eventType);
        var handler = _serviceProvider.GetService(handlerType);

        if (handler is null)
        {
            _logger.LogInformation("No handler registered for event type '{EventType}'. Event marked as processed.", eventTypeName);
            return;
        }

        var method = handlerType.GetMethod(nameof(IOutboxEventHandler<OutboxEventBase>.HandleAsync))!;
        await (Task)method.Invoke(handler, [@event, cancellationToken])!;
    }

    public async Task DispatchFailureAsync(
        string eventTypeName,
        string payload,
        CancellationToken cancellationToken)
    {
        var eventType = OutboxEventTypeRegistry.Resolve(eventTypeName);
        if (eventType is null)
        {
            return;
        }

        var @event = JsonSerializer.Deserialize(payload, eventType)
            as OutboxEventBase;
        if (@event is null)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize payload for event type '{eventTypeName}'.");
        }

        var handlerType = typeof(IOutboxEventFailureHandler<>)
            .MakeGenericType(eventType);
        var handler = _serviceProvider.GetService(handlerType);
        if (handler is null)
        {
            return;
        }

        var method = handlerType.GetMethod(
            nameof(IOutboxEventFailureHandler<OutboxEventBase>
                .HandleFailureAsync))!;
        await (Task)method.Invoke(
            handler,
            [@event, cancellationToken])!;
    }
}
