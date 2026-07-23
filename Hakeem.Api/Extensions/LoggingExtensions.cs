using Hakeem.Api.Configuration;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Hakeem.Api.Extensions;


public static class LoggingExtensions
{

    public static WebApplicationBuilder AddLoggingServices(this WebApplicationBuilder builder)
    {


        // Configure Serilog
        Log.Logger = CreateSerilogLogger(builder.Configuration, builder.Environment);

        // Clear default logging providers and add Serilog
        builder.Logging.ClearProviders();
        builder.Host.UseSerilog();


        return builder;
    }


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


}