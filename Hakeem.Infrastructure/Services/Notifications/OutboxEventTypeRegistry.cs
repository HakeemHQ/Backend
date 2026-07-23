using System.Reflection;
using Hakeem.Domain.DomainEvents.Outbox;

namespace Hakeem.Infrastructure.Services;

/// <summary>
/// Static registry that maps event type names (e.g. "WelcomeEmailEvent") to their concrete CLR types.
/// Used by the <see cref="OutboxEventDispatcher"/> to deserialize outbox rows back into typed event objects.
/// </summary>
public static class OutboxEventTypeRegistry
{
    private static readonly Dictionary<string, Type> _typeMap = new();

    /// <summary>
    /// Scans an assembly for all concrete classes that extend <see cref="OutboxEventBase"/>
    /// and registers them by their simple type name.
    /// </summary>
    public static void RegisterFromAssembly(Assembly assembly)
    {
        var eventTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsAssignableTo(typeof(OutboxEventBase)));

        foreach (var type in eventTypes)
        {
            _typeMap[type.Name] = type;
        }
    }

    /// <summary>
    /// Resolves a concrete event type from its simple name.
    /// Returns null if no type is registered with that name.
    /// </summary>
    public static Type? Resolve(string eventTypeName)
        => _typeMap.GetValueOrDefault(eventTypeName);
}
