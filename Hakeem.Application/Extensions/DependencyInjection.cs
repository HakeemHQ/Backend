using System.Reflection;
using FluentValidation;
using FluentValidation.AspNetCore;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Hakeem.Application.Abstractions;
using Hakeem.Application.Configurations;
using Hakeem.Application.Interfaces.Processors;
using Hakeem.Application.Services.DocumentExtraction;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var assembly = Hakeem.Application.AssemblyReference.Assembly;

        AddGeminiChat(services, configuration);
        AddMedicalCvPreviewLinks(services, configuration);

        services.RegisterServicesWithLifetime(assembly);
        services.AddScoped<
            IDocumentExtractionProcessor,
            DocumentExtractionProcessor>();
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

    private static void AddGeminiChat(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetRequiredSection(
            GeminiChatConfiguration.SectionName);

        services.AddOptions<GeminiChatConfiguration>()
            .Bind(section)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ModelId),
                "GeminiChat ModelId must be configured.")
            .Validate(
                options => options.MaxTokens > 0,
                "GeminiChat MaxTokens must be greater than zero.")
            .Validate(
                options => options.MedicalCvMaxTokens is > 0 and <= 65_536,
                "GeminiChat MedicalCvMaxTokens must be between 1 and 65536.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ApiKey),
                "GeminiChat ApiKey must be configured.")
            .Validate(
                options => options.MaxAgentIterations is > 0 and <= 4,
                "GeminiChat MaxAgentIterations must be between 1 and 4.")
            .Validate(
                options => options.AgentTimeoutSeconds > 0,
                "GeminiChat AgentTimeoutSeconds must be greater than zero.")
            .ValidateOnStart();
    }

    private static void AddMedicalCvPreviewLinks(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<MedicalCvPreviewLinkConfiguration>()
            .Bind(configuration.GetSection(
                MedicalCvPreviewLinkConfiguration.SectionName))
            .Validate(
                options => options.LifetimeMinutes is > 0 and <= 60,
                "Medical CV preview link lifetime must be between 1 and 60 minutes.")
            .ValidateOnStart();
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
