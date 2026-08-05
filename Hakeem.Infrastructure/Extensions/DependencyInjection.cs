using System.Reflection;
using Azure;
using Azure.AI.DocumentIntelligence;
using Hakeem.Application.Configurations;
using Hakeem.Application.Interfaces.Agents;
using Hakeem.Application.Interfaces.Rag;
using Hakeem.Domain.DomainEvents.Outbox;
using Hakeem.Domain.Interfaces.ServiceLifetime;
using Hakeem.Infrastructure.AI.Agents;
using Hakeem.Infrastructure.AI.Chat;
using Hakeem.Infrastructure.Context;
using Hakeem.Infrastructure.Rag;
using Hakeem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using Qdrant.Client;

namespace Hakeem.Infrastructure.Extensions;

public static class DependencyInjection
{
    private const string GeminiChatHttpClient = "GeminiChat";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment)
    {
        services.RegisterServicesWithLifetime(Assembly.GetExecutingAssembly());
        services.AddScoped<
            IDocumentProcessingAgent,
            DocumentProcessingAgent>();
        services.AddScoped<
            IDocumentProcessingWorkflow,
            DocumentProcessingWorkflow>();
        AddHuggingFaceEmbedding(services, configuration);
        AddQdrant(services, configuration);
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

        services
            .AddHttpClient(GeminiChatHttpClient)
            .AddHttpMessageHandler(
                () => new GeminiRequiredToolCallHandler());
        services.AddSingleton<IChatCompletionService>(
            serviceProvider =>
            {
                var httpClientFactory =
                    serviceProvider.GetRequiredService<
                        IHttpClientFactory>();
                var options = serviceProvider
                    .GetRequiredService<
                        IOptions<GeminiChatConfiguration>>()
                    .Value;

#pragma warning disable SKEXP0070
                return new GoogleAIGeminiChatCompletionService(
                    options.ModelId,
                    options.ApiKey,
                    GoogleAIVersion.V1_Beta,
                    httpClientFactory.CreateClient(
                        GeminiChatHttpClient),
                    loggerFactory: null);
#pragma warning restore SKEXP0070
            });

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

    private static void AddHuggingFaceEmbedding(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(
            HuggingFaceEmbeddingConfiguration.SectionName);

        services.AddOptions<HuggingFaceEmbeddingConfiguration>()
            .Bind(section)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.EmbeddingsEndpoint),
                "HuggingFaceEmbedding EmbeddingsEndpoint must be configured.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ModelId),
                "HuggingFaceEmbedding ModelId must be configured.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ApiKey),
                "HuggingFaceEmbedding ApiKey must be configured.")
            .Validate(
                options => options.Dimensions > 0,
                "HuggingFaceEmbedding Dimensions must be greater than zero.")
            .ValidateOnStart();

        services
            .AddHttpClient<HuggingFaceEmbeddingService>(client =>
                client.Timeout = TimeSpan.FromSeconds(120));

        services.AddScoped<IEmbeddingService, HuggingFaceEmbeddingService>();
        services.AddScoped<
            IMedicalRecordFieldVectorStore,
            QdrantMedicalRecordFieldVectorStore>();
    }

    private static void AddQdrant(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(QdrantConfiguration.SectionName);

        services.AddOptions<QdrantConfiguration>()
            .Bind(section)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Host),
                "Qdrant Host must be configured.")
            .Validate(
                options => options.Port > 0,
                "Qdrant Port must be greater than zero.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.CollectionName),
                "Qdrant CollectionName must be configured.")
            .Validate(
                options => options.VectorSize > 0,
                "Qdrant VectorSize must be greater than zero.")
            .ValidateOnStart();

        services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<QdrantConfiguration>>()
                .Value;
            var connection = options.GetConnectionSettings();

            return string.IsNullOrWhiteSpace(options.ApiKey)
                ? new QdrantClient(
                    connection.Host,
                    connection.Port,
                    https: connection.UseTls)
                : new QdrantClient(
                    connection.Host,
                    connection.Port,
                    https: connection.UseTls,
                    apiKey: options.ApiKey);
        });
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
