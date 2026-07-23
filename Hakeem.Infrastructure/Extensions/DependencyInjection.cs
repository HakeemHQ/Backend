using System.Reflection;
using Hakeem.Domain.Interfaces.ServiceLifetime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Hakeem.Domain.DomainEvents.Outbox;
using Hakeem.Infrastructure.Services;
using Hakeem.Infrastructure.Context;

namespace Hakeem.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment)
    {
        services.RegisterServicesWithLifetime(Assembly.GetExecutingAssembly());
        // Configure Entity Framework DbContext
        var cmsDbConnectionString = configuration.GetConnectionString("CMSConnection")!;
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(cmsDbConnectionString);
            // Enable sensitive data logging in development
            if (isDevelopment)
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        services.AddHttpClient();




        // Register outbox event infrastructure
        OutboxEventTypeRegistry.RegisterFromAssembly(typeof(OutboxEventBase).Assembly);
        services.AddScoped<OutboxEventDispatcher>();
        services.AddHostedService<OutboxEventProcessor>();

        return services;
    }
    private static void RegisterServicesWithLifetime(
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

    }
}
