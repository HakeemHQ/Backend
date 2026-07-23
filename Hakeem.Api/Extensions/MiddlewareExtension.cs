using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Constants;
using Hakeem.Application.Resources;
using Hakeem.Api.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
namespace Hakeem.Api.Extensions;

public static class MiddlewareExtension
{

    public static WebApplication UseWebApiMiddleware(this WebApplication app)
    {
        var headersConfig = app.Services.GetRequiredService<IOptions<HeadersConfiguration>>().Value;
        app.Use(async (context, next) =>
       {
           var headers = context.Response.Headers;

           headers["Content-Security-Policy"] = headersConfig.ContentSecurityPolicy;
           headers["X-Content-Type-Options"] = "nosniff";
           headers["X-Frame-Options"] = "DENY"; // protects against Clickjacking
           headers["Referrer-Policy"] = headersConfig.ReferrerPolicy;
           headers["Permissions-Policy"] = headersConfig.PermissionsPolicy;

           await next();
       });
        // Configure error handling based on environment

        app.UseExceptionHandler();


        // Force HTTPS redirection
        app.UseHttpsRedirection();

        // Configure HSTS for production
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        // Configure Swagger with versioning support
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("v1/swagger.json", "Hakeem API v1");
            c.RoutePrefix = "swagger";
            c.DocumentTitle = "Hakeem API Documentation";
            c.DefaultModelsExpandDepth(-1); // Hide models section by default
            c.DisplayRequestDuration();
            c.EnableDeepLinking();
            c.EnableFilter();
            c.ShowExtensions();
            c.EnableValidator();
        });

        // Configure localization
        app.UseRequestLocalization();

        // Configure CORS with security settings (must be before StaticFiles and UseRouting)
        app.UseCors("SecurityPolicy");

        // Enable serving static files (for uploaded files in wwwroot)
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = context =>
            {
                if (context.Context.Request.Path.StartsWithSegments("/uploads"))
                {
                    var headers = context.Context.Response.Headers;

                    headers["Cross-Origin-Resource-Policy"] = "cross-origin";
                    headers.Remove("Cross-Origin-Embedder-Policy");
                    headers.Remove("Cross-Origin-Opener-Policy");

                    headers["Content-Security-Policy"] =
                        "default-src 'self'; img-src 'self' data: https:;";

                    headers["X-Content-Type-Options"] = "nosniff";
                    headers.Remove("Set-Cookie");
                }
            }
        });

        app.UseRouting();

        // Authentication and authorization (order matters!).
        app.UseAuthentication();
        app.UseAuthorization();
        // Map controllers
        app.MapControllers();

        // Add graceful shutdown handling
        app.Lifetime.ApplicationStopping.Register(() =>
        {
            app.Logger.LogInformation("Application is shutting down gracefully...");
        });

        return app;
    }




    public static WebApplication UseCustomErrorPages(this WebApplication app)
    {
        app.UseStatusCodePages(async context =>
        {
            var response = context.HttpContext.Response;
            var localizer = context.HttpContext.RequestServices
                .GetRequiredService<IStringLocalizer<SharedResource>>();

            response.ContentType = "application/json";

            if (response.StatusCode == 404)
            {
                var notFoundResponse = GenericResponseModel<object>.Failure(
                    localizer[ErrorCodes.ResourceNotFound]);
                await response.WriteAsJsonAsync(notFoundResponse);
            }
            else if (response.StatusCode == 401)
            {
                var unauthorizedResponse = GenericResponseModel<object>.Failure(
                    localizer[ErrorCodes.AuthUnauthorized]);
                await response.WriteAsJsonAsync(unauthorizedResponse);
            }
            else if (response.StatusCode == 403)
            {
                var forbiddenResponse = GenericResponseModel<object>.Failure(
                    localizer[ErrorCodes.AuthForbidden]);
                await response.WriteAsJsonAsync(forbiddenResponse);
            }
        });

        return app;
    }

}