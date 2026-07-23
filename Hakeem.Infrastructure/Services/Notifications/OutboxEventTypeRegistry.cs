using System.Reflection;
using Hakeem.Domain.DomainEvents.Outbox;

namespace Hakeem.Infrastructure.Services;

public static class OutboxEventTypeRegistry
{
    private static readonly Dictionary<string, Type> _typeMap = new();


    public static void RegisterFromAssembly(Assembly assembly)
    {
        var eventTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsAssignableTo(typeof(OutboxEventBase)));

        foreach (var type in eventTypes)
        {
            _typeMap[type.Name] = type;
        }
    }

    public static Type? Resolve(string eventTypeName)
        => _typeMap.GetValueOrDefault(eventTypeName);
}
