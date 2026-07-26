using System.Text;
using Hakeem.Api.Configuration;
using Hakeem.Application.Repositories.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Hakeem.Api.Extensions;


public static class SecurityExtensions
{
    public static IServiceCollection AddSecurityServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add and validate security configuration
        var securityConfig = configuration.GetSection(SecurityConfiguration.SectionName).Get<SecurityConfiguration>()
            ?? throw new InvalidOperationException("Security configuration is missing");

        // Configure JWT authentication
        services.AddJwtAuthentication(securityConfig.Jwt);

        // Register JWT configuration options for token services (Application layer)
        services.Configure<Application.Configurations.JwtConfiguration>(options =>
        {
            options.Key = securityConfig.Jwt.Key;
            options.Issuer = securityConfig.Jwt.Issuer;
            options.Audience = securityConfig.Jwt.Audience;
            options.TokenExpirationMinutes = securityConfig.Jwt.TokenExpirationMinutes;
            options.RefreshTokenExpirationDays = securityConfig.Jwt.RefreshTokenExpirationDays;
        });

        // Register security header configuration
        services.Configure<HeadersConfiguration>(configuration.GetSection($"{SecurityConfiguration.SectionName}:Headers"));

        // Configure CORS
        services.AddCorsConfiguration(securityConfig.Cors);

        return services;
    }

    private static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        Hakeem.Api.Configuration.JwtConfiguration jwtConfig)
    {
        if (string.IsNullOrEmpty(jwtConfig.Key) || jwtConfig.Key.Length < 32)
        {
            throw new InvalidOperationException("JWT Key must be at least 32 characters long");
        }

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = jwtConfig.RequireHttpsMetadata;
                options.SaveToken = jwtConfig.SaveToken;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = jwtConfig.ValidateLifetime,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = jwtConfig.Issuer,
                    ValidAudience = jwtConfig.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtConfig.Key)),

                    ClockSkew = jwtConfig.ClockSkew,

                    // Additional security settings
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    RequireAudience = true
                };

                // Configure JWT events for logging and security monitoring
                options.Events = new JwtBearerEvents
                {

                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<JwtBearerEvents>>();

                        logger.LogWarning("JWT Authentication failed: {Error} for {User} from {IP}",
                            context.Exception.Message,
                            context.HttpContext.User?.Identity?.Name ?? "Anonymous",
                            context.HttpContext.Connection.RemoteIpAddress);

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<JwtBearerEvents>>();

                        var userIdClaim = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                        var jwtId = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);

                        if (!Guid.TryParse(userIdClaim, out var userId) ||
                            string.IsNullOrWhiteSpace(jwtId))
                        {
                            context.Fail("The access token is missing required session claims.");
                            return;
                        }

                        var refreshTokenRepository = context.HttpContext.RequestServices
                            .GetRequiredService<IRefreshTokenRepository>();
                        var isSessionActive = await refreshTokenRepository.IsSessionActiveAsync(
                            jwtId,
                            userId,
                            context.HttpContext.RequestAborted);

                        if (!isSessionActive)
                        {
                            logger.LogWarning(
                                "Revoked or inactive JWT rejected for user {UserId} from {IP}",
                                userId,
                                context.HttpContext.Connection.RemoteIpAddress);
                            context.Fail("The access token session has been revoked.");
                            return;
                        }

                        logger.LogInformation("JWT Token validated for user: {User}",
                            context.Principal?.Identity?.Name ?? "Unknown");
                    },
                    OnChallenge = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<JwtBearerEvents>>();

                        logger.LogWarning("JWT Challenge triggered for path: {Path} from IP: {IP}",
                            context.Request.Path,
                            context.HttpContext.Connection.RemoteIpAddress);

                        return Task.CompletedTask;
                    }
                };
            });

        // Configure comprehensive authorization
        services.AddAuthorization(options =>
        {
            // Default policy requiring authentication
            options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

        });
        return services;
    }


    private static IServiceCollection AddCorsConfiguration(
        this IServiceCollection services,
        CorsConfiguration corsConfig)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("SecurityPolicy", policy =>
            {
                if (corsConfig.AllowedOrigins.Any())
                {
                    policy.WithOrigins(corsConfig.AllowedOrigins.ToArray());
                }
                else
                {
                    // If credentials are enabled, we cannot use AllowAnyOrigin()
                    // Use a default set of common development origins
                    if (corsConfig.AllowCredentials)
                    {
                        policy.WithOrigins("http://localhost:3000", "http://localhost:3001", "http://localhost:4200", "http://localhost:8080");
                    }
                    else
                    {
                        policy.AllowAnyOrigin();
                    }
                }

                if (corsConfig.AllowedMethods.Any())
                {
                    policy.WithMethods(corsConfig.AllowedMethods.ToArray());
                }
                else
                {
                    policy.AllowAnyMethod();
                }

                if (corsConfig.AllowedHeaders.Any())
                {
                    policy.WithHeaders(corsConfig.AllowedHeaders.ToArray());
                }
                else
                {
                    policy.AllowAnyHeader();
                }

                if (corsConfig.AllowCredentials)
                {
                    policy.AllowCredentials();
                }

                policy.SetPreflightMaxAge(TimeSpan.FromSeconds(corsConfig.PreflightMaxAge));
            });
        });

        return services;
    }

}
