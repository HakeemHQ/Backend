using System.Globalization;
using Hakeem.Api.Configuration;
using Hakeem.Api.Handlers;
using Microsoft.AspNetCore.Localization;

namespace Hakeem.Api.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures comprehensive web API services with security, monitoring, and validation
    /// </summary>
    public static IServiceCollection AddWebApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add security services
        services.AddSecurityServices(configuration);

        // Add exception handling
        services.AddExceptionHandler<GlobalExceptionHandler>();


        // Configure localization for government requirements (Arabic/English)
        services.AddLocalization();
        services.Configure<RequestLocalizationOptions>(opts =>
        {
            var supportedCultures = new[]
            {
                new CultureInfo("en"),
                new CultureInfo("ar")
            };
            var supportedUICultures = new[]
            {
                new CultureInfo("en"),
                new CultureInfo("ar")
            };
            opts.DefaultRequestCulture = new RequestCulture("en");
            opts.SupportedCultures = supportedCultures;
            opts.SupportedUICultures = supportedUICultures;

            // Configure culture providers
            opts.RequestCultureProviders = new List<IRequestCultureProvider>
            {
                new QueryStringRequestCultureProvider(),
                new CookieRequestCultureProvider(),
                new AcceptLanguageHeaderRequestCultureProvider()
            };
        });


        // services.AddMemoryCache();
        services.AddEndpointsApiExplorer();

        // Add Swagger services
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Hakeem API",
                Version = "v1",
                Description = "Financial Authority Audit API — E-service endpoints use Bearer JWT; CMS endpoints use X-Email (dev) or CmsAzureAd Bearer (prod).",
                Contact = new Microsoft.OpenApi.Models.OpenApiContact
                {
                    Name = "Hakeem Support",
                    Email = "support@Hakeem.gov.ae"
                },
                License = new Microsoft.OpenApi.Models.OpenApiLicense
                {
                    Name = "Government License"
                }
            });

            // ── 1. Default JWT Bearer – E-service endpoints ───────────────────────
            c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Description = "**E-Service JWT** — paste the token from /api/v1/Auth/login.\n\nFormat: `Bearer {token}`",
                Name = "Authorization",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            // ── 4. Global Security Requirements ──────────────────────────────────
            // Adding these globally ensures Swagger UI sends the headers if authorized,
            // even without per-operation locks.
            c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                },
            });

            // Include XML comments if available
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, xmlFile);
            if (System.IO.File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }

            // Use full class names for schema IDs to avoid conflicts
            c.CustomSchemaIds(type => type.FullName);
        });



        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Instance = context.HttpContext.Request.Path;
                context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);
                context.ProblemDetails.Extensions.TryAdd("timestamp", DateTime.UtcNow);
            };
        });

        return services;
    }

}
