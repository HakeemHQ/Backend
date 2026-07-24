using System.Text.Json;
using System.Text.Json.Serialization;
using Hakeem.Api.Authentication;
using Hakeem.Api.Extensions;
using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Configurations;
using Hakeem.Application.Constants;
using Hakeem.Application.Extensions;
using Hakeem.Application.Resources;
using Hakeem.Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;

namespace Hakeem.Api;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Starting Hakeem API application");

            var builder = WebApplication.CreateBuilder(args);

            builder.AddLoggingServices();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddMemoryCache();
            builder.Services
                .AddWebApiServices(builder.Configuration)
                .AddApplication()
                .AddInfrastructure(builder.Configuration, builder.Environment.IsProduction());

            builder.Services.Configure<FileStorageConfiguration>(
                builder.Configuration.GetSection(FileStorageConfiguration.SectionName));

            builder.Services.Configure<FileSettingsConfiguration>(
                builder.Configuration.GetSection(FileSettingsConfiguration.SectionName));

            builder.Services.Configure<EmailSettingsConfiguration>(
                builder.Configuration.GetSection(EmailSettingsConfiguration.SectionName));

            builder.Services.Configure<SmsConfiguration>(
                builder.Configuration.GetSection(SmsConfiguration.SectionName));

            builder.Services.Configure<OutboxConfiguration>(
                builder.Configuration.GetSection(OutboxConfiguration.SectionName));


            builder.Services.AddControllers()
            .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = context => ValidationResult(context))
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

                    options.JsonSerializerOptions.MaxDepth = 64;

                });

            builder.Services.AddScoped<
                ICurrentUserContext,
                DevelopmentCurrentUserContext>();

            var app = builder.Build();

            await ApplyDatabaseMigrationsAsync(app.Services);

            LogStartupInformation(app);

            app.UseWebApiMiddleware();

            if (!app.Environment.IsDevelopment())
            {
                app.UseCustomErrorPages();
            }

            Log.Information("Hakeem API application started successfully");

            await app.RunAsync();

            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Hakeem API application terminated unexpectedly");
            return 1;
        }

        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }


    private static void LogStartupInformation(WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("=== Hakeem API Startup Information ===");
        logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
        logger.LogInformation("Content Root: {ContentRoot}", app.Environment.ContentRootPath);
        logger.LogInformation("Web Root: {WebRoot}", app.Environment.WebRootPath);
        logger.LogInformation("Assembly Version: {Version}",
            System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
        logger.LogInformation("Machine Name: {MachineName}", Environment.MachineName);
        logger.LogInformation("OS Version: {OSVersion}", Environment.OSVersion);
        logger.LogInformation("Processor Count: {ProcessorCount}", Environment.ProcessorCount);
        logger.LogInformation("Working Set: {WorkingSet} MB",
            Environment.WorkingSet / (1024 * 1024));

        // Log configuration sources (for debugging)
        var configuration = app.Services.GetRequiredService<IConfiguration>();
        if (configuration is IConfigurationRoot configRoot)
        {
            logger.LogInformation("Configuration Sources:");
            foreach (var provider in configRoot.Providers)
            {
                logger.LogInformation("  - {ProviderType}: {Provider}",
                    provider.GetType().Name, provider.ToString());
            }
        }

        logger.LogInformation("=====================================");
    }


    private static async Task ApplyDatabaseMigrationsAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        try
        {
            // Check if auto-migration is enabled
            var autoMigrate = configuration.GetValue<bool>("Database:AutoMigrate", true);
            if (!autoMigrate)
            {
                logger.LogInformation("Auto-migration is disabled. Skipping database migration.");
                return;
            }

            logger.LogInformation("Starting automatic database migration...");

            await ApplyMigrationsForContextAsync(scope, configuration, logger, typeof(Hakeem.Infrastructure.Context.ApplicationDbContext), "ApplicationDbContext");

            logger.LogInformation("Database migration completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to apply database migrations. Application startup will continue but database may be in an inconsistent state.");

        }
    }


    private static async Task ApplyMigrationsForContextAsync(IServiceScope scope, IConfiguration configuration, ILogger<Program> logger, Type contextType, string displayName)
    {
        var contextSection = configuration.GetSection("Database:Contexts").GetSection(displayName);
        var autoMigrate = contextSection.GetValue<bool?>("AutoMigrate")
            ?? configuration.GetValue("Database:AutoMigrate", true);
        if (!autoMigrate)
        {
            logger.LogInformation("Auto-migration disabled for {Context}. Skipping.", displayName);
            return;
        }

        if (scope.ServiceProvider.GetService(contextType) is not DbContext dbContext)
        {
            logger.LogWarning("DbContext {Context} is not registered. Skipping migration.", displayName);
            return;
        }

        var connection = dbContext.Database.GetDbConnection();

        logger.LogCritical(
            "ACTUAL DATABASE => Server: {Server} | Database: {Database} | ConnectionString: {ConnectionString}",
            connection.DataSource,
            connection.Database,
            connection.ConnectionString);

        logger.LogInformation("Applying migrations for {Context}...", displayName);

        var canConnect = await dbContext.Database.CanConnectAsync();
        logger.LogInformation("{Context} connection test: {CanConnect}", displayName, canConnect);
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        logger.LogInformation("{Context} pending migrations count: {Count}", displayName, pendingMigrations.Count());
        if (pendingMigrations.Any())
        {
            logger.LogInformation("{Context} pending migrations: {Migrations}", displayName, string.Join(", ", pendingMigrations));
        }

        var timeoutSeconds = contextSection.GetValue<int?>("MigrationTimeoutSeconds")
            ?? configuration.GetValue("Database:MigrationTimeoutSeconds", 300);
        var retryCount = contextSection.GetValue<int?>("RetryCount")
            ?? configuration.GetValue("Database:RetryCount", 3);
        var retryDelaySeconds = contextSection.GetValue<int?>("RetryDelaySeconds")
            ?? configuration.GetValue("Database:RetryDelaySeconds", 5);

        await RetryOperationAsync(async () =>
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            await dbContext.Database.MigrateAsync(cts.Token);
        }, retryCount, TimeSpan.FromSeconds(retryDelaySeconds), logger);
    }


    private static async Task RetryOperationAsync(Func<Task> operation, int maxRetries, TimeSpan delay, Microsoft.Extensions.Logging.ILogger logger)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await operation();
                return;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                logger.LogWarning(ex, "Migration attempt {Attempt} failed. Retrying in {Delay} seconds...",
                    attempt, delay.TotalSeconds);
                await Task.Delay(delay);
                delay = TimeSpan.FromSeconds(delay.TotalSeconds * 2); // Exponential backoff
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Migration failed after {MaxRetries} attempts.", maxRetries);
                throw;
            }
        }
    }
    static BadRequestObjectResult ValidationResult(ActionContext context)
    {
        var localizer = context.HttpContext.RequestServices
            .GetRequiredService<IStringLocalizer<SharedResource>>();

        var errorList = context.ModelState
            .Where(state => state.Value?.ValidationState == ModelValidationState.Invalid)
            .SelectMany(
                state => state.Value?.Errors ?? Enumerable.Empty<ModelError>(),
                (state, error) => ErrorResponseModel.Create(
                    state.Key,
                    !string.IsNullOrWhiteSpace(error.ErrorMessage) ? error.ErrorMessage : localizer[ErrorCodes.ValidationRequired].Value,
                    ErrorCodes.ValidationRequired))
            .ToList();

        return new BadRequestObjectResult(
            GenericResponseModel<object>.Failure(localizer["Validation.Error"], errorList));
    }

}
