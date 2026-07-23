using System.Reflection;
using FluentValidation;
using FluentValidation.AspNetCore;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Hakeem.Application.Abstractions;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Hakeem.Application.AssemblyReference.Assembly;

        services.RegisterServicesWithLifetime(assembly);
        services.AddValidatorsFromAssembly(assembly);
        services.AddFluentValidationAutoValidation();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(assembly);
        });

        // Auto-register all IOutboxEventHandler<> implementations as scoped
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes
                .AssignableTo(typeof(IOutboxEventHandler<>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(assembly);
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        return services;
    }

    private static IServiceCollection RegisterServicesWithLifetime(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(classes => classes
                .AssignableTo<IScoped>())
            .AsImplementedInterfaces()
            .WithScopedLifetime()
            .AddClasses(classes => classes
                .AssignableTo<ISingleton>())
            .AsImplementedInterfaces()
            .WithSingletonLifetime()
            .AddClasses(classes => classes
                .AssignableTo<ITransient>())
            .AsImplementedInterfaces()
            .WithTransientLifetime());

        return services;
    }
}
