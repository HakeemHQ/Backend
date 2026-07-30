using System.Reflection;
using Azure;
using Azure.AI.DocumentIntelligence;
using Hakeem.Application.Configurations;
using Hakeem.Application.Interfaces.Agents;
using Hakeem.Domain.DomainEvents.Outbox;
using Hakeem.Domain.Interfaces.ServiceLifetime;
using Hakeem.Infrastructure.AI.Agents;
using Hakeem.Infrastructure.Context;
using Hakeem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hakeem.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment)
    {
        services.RegisterServicesWithLifetime(Assembly.GetExecutingAssembly());
        services.AddScoped<
            IDocumentProcessingAgent,
            DocumentProcessingAgent>();
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

        services.AddHttpClient();

        services.AddOptions<AzureDocumentIntelligenceOptions>()
            .Bind(configuration.GetSection(
                AzureDocumentIntelligenceOptions.SectionName))
            .Validate(
                options =>
                    Uri.TryCreate(
                        options.Endpoint,
                        UriKind.Absolute,
                        out var endpoint) &&
                    endpoint.Scheme == Uri.UriSchemeHttps,
                "Azure Document Intelligence Endpoint must be a configured HTTPS URI.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ApiKey),
                "Azure Document Intelligence ApiKey must be configured.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ModelId),
                "Azure Document Intelligence ModelId must be configured.")
            .ValidateOnStart();

        services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider
                .GetRequiredService<
                    IOptions<AzureDocumentIntelligenceOptions>>()
                .Value;

            var clientOptions = new DocumentIntelligenceClientOptions();
            clientOptions.Diagnostics.IsLoggingContentEnabled = false;

            return new DocumentIntelligenceClient(
                new Uri(options.Endpoint),
                new AzureKeyCredential(options.ApiKey),
                clientOptions);
        });

        // Register outbox event infrastructure
        OutboxEventTypeRegistry.RegisterFromAssembly(
            typeof(OutboxEventBase).Assembly);
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
