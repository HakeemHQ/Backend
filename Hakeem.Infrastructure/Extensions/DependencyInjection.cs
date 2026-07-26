using Hakeem.Application.Interfaces.Notifications;
using Hakeem.Domain.DomainEvents.Outbox;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces.ServiceLifetime;
using Hakeem.Infrastructure.Context;
using Hakeem.Infrastructure.Services;
using Hakeem.Infrastructure.Services.Notifications;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Hakeem.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration,
        bool isDevelopment)
    {
        services.RegisterServicesWithLifetime(Assembly.GetExecutingAssembly());
        // Configure Entity Framework DbContext
        var DbConnectionString = configuration.GetConnectionString("DefaultConnection")!;
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(DbConnectionString);
            // Enable sensitive data logging in development
            if (isDevelopment)
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        services.AddIdentityCore<User>(options =>
        {
            options.Password.RequiredLength = 6;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddSignInManager()
        .AddDefaultTokenProviders();

        services.Configure<ClientAppSettings>(
        configuration.GetSection("ClientAppSettings"));

        services.AddSingleton<IClientAppSettings>(sp =>
        sp.GetRequiredService<IOptions<ClientAppSettings>>().Value);

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
