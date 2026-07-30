using System.Reflection;
using FluentValidation;
using FluentValidation.AspNetCore;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Hakeem.Application.Abstractions;
using Hakeem.Application.Configurations;
using Hakeem.Domain.Interfaces.ServiceLifetime;
using Microsoft.SemanticKernel;

namespace Hakeem.Application.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var assembly = Hakeem.Application.AssemblyReference.Assembly;

        AddDocumentExtractionAi(services, configuration);

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

    private static void AddDocumentExtractionAi(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetRequiredSection(
            DocumentExtractionAiConfiguration.SectionName);

        services.AddOptions<DocumentExtractionAiConfiguration>()
            .Bind(section)
            .Validate(
                options => Uri.TryCreate(
                    options.BaseUrl,
                    UriKind.Absolute,
                    out _),
                "DocumentExtractionAi BaseUrl must be an absolute URI.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(
                    options.ChatEndpoint),
                "DocumentExtractionAi ChatEndpoint must be configured.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ModelId),
                "DocumentExtractionAi ModelId must be configured.")
            .Validate(
                options => options.MaxTokens > 0,
                "DocumentExtractionAi MaxTokens must be greater than zero.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ApiKey),
                "DocumentExtractionAi ApiKey must be configured.")
            .ValidateOnStart();

        var options = section
            .Get<DocumentExtractionAiConfiguration>()
            ?? throw new InvalidOperationException(
                $"Configuration section '{DocumentExtractionAiConfiguration.SectionName}' is invalid.");

#pragma warning disable SKEXP0010
        services.AddOpenAIChatCompletion(
            modelId: options.ModelId,
            endpoint: options.GetChatEndpointUri(),
            apiKey: options.ApiKey,
            serviceId: DocumentExtractionAiConfiguration.ServiceId);
#pragma warning restore SKEXP0010
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
