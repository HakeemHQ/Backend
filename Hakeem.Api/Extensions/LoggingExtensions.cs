using Hakeem.Api.Configuration;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Hakeem.Api.Extensions;

/// <summary>
/// Logging configuration extensions for structured logging with Serilog
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Configures comprehensive logging with Serilog
    /// </summary>
    public static WebApplicationBuilder AddLoggingServices(this WebApplicationBuilder builder)
    {


        // Configure Serilog
        Log.Logger = CreateSerilogLogger(builder.Configuration, builder.Environment);

        // Clear default logging providers and add Serilog
        builder.Logging.ClearProviders();
        builder.Host.UseSerilog();


        return builder;
    }

    /// <summary>
    /// Creates a configured Serilog logger with comprehensive enrichment and sinks
    /// </summary>
    private static Logger CreateSerilogLogger(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var loggerConfig = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Hakeem.Api")
            .Enrich.WithProperty("Version", "1.0.0")
            .Enrich.WithProperty("Environment", environment.EnvironmentName)
            .Enrich.WithProcessId()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("ClientIp", "Unknown")

            .Filter.ByExcluding(ShouldExcludeLog);

        // Add environment-specific configuration
        if (environment.IsDevelopment())
        {
            loggerConfig.MinimumLevel.Debug();
        }
        else
        {
            loggerConfig.MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning);
        }

        return loggerConfig.CreateLogger();
    }

    /// <summary>
    /// Determines if a log event should be excluded (e.g., health checks, sensitive data)
    /// </summary>
    private static bool ShouldExcludeLog(LogEvent logEvent)
    {
        // Exclude health check endpoints from logs to reduce noise
        if (logEvent.Properties.ContainsKey("RequestPath"))
        {
            var requestPath = logEvent.Properties["RequestPath"].ToString();
            if (requestPath.Contains("/health") || requestPath.Contains("/metrics"))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Adds structured request logging with security considerations
    /// </summary>

    // public static WebApplication UseStructuredRequestLogging(this WebApplication app)
    // {
    //     app.UseSerilogRequestLogging(options =>
    //     {
    //         options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

    //         options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    //         {
    //             diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? "unknown");
    //             diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme ?? "unknown");
    //             diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.FirstOrDefault()?.ToString() ?? "unknown");
    //             diagnosticContext.Set("ClientIp", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
    //             diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier ?? "unknown");

    //             // Add user information if authenticated
    //             if (httpContext.User?.Identity?.IsAuthenticated == true)
    //             {
    //                 diagnosticContext.Set("UserId", httpContext.User.Identity.Name ?? "unknown");
    //             }

    //             // Add response size
    //             if (httpContext.Response.ContentLength.HasValue)
    //             {
    //                 diagnosticContext.Set("ResponseSize", httpContext.Response.ContentLength.Value);
    //             }
    //         };

    //         // Configure different log levels for different status codes
    //         options.GetLevel = (httpContext, elapsed, ex) =>
    //         {
    //             if (ex != null || httpContext.Response.StatusCode > 499)
    //                 return LogEventLevel.Error;

    //             if (httpContext.Response.StatusCode > 399)
    //                 return LogEventLevel.Warning;

    //             if (elapsed > 5000) // Slow requests over 5 seconds
    //                 return LogEventLevel.Warning;

    //             return LogEventLevel.Information;
    //         };
    //     });

    //     return app;
    // }
}