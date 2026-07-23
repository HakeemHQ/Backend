using System.ComponentModel.DataAnnotations;

namespace Hakeem.Api.Configuration;

public class SecurityConfiguration
{
    public const string SectionName = "Security";

    [Required]
    public JwtConfiguration Jwt { get; set; } = new();

    [Required]
    public CorsConfiguration Cors { get; set; } = new();
    [Required]
    public HeadersConfiguration Headers { get; set; } = new();
}


public class JwtConfiguration
{
    [Required]
    [MinLength(32, ErrorMessage = "JWT Key must be at least 32 characters long")]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Range(1, 1440, ErrorMessage = "Token expiration must be between 1 and 1440 minutes")]
    public int TokenExpirationMinutes { get; set; } = 60;

    [Range(1, 30, ErrorMessage = "Refresh token expiration must be between 1 and 30 days")]
    public int RefreshTokenExpirationDays { get; set; } = 7;

    public bool RequireHttpsMetadata { get; set; } = true;
    public bool SaveToken { get; set; } = false;
    public bool ValidateLifetime { get; set; } = true;
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);
}


public class CorsConfiguration
{
    [Required]
    public List<string> AllowedOrigins { get; set; } = new();

    public List<string> AllowedMethods { get; set; } = new() { "GET", "POST", "PUT", "DELETE" };

    public List<string> AllowedHeaders { get; set; } = new() { "Content-Type", "Authorization" };

    public bool AllowCredentials { get; set; } = false;

    public int PreflightMaxAge { get; set; } = 86400; // 24 hours
}


public class HeadersConfiguration
{
    public bool EnableHsts { get; set; } = false;

    public int HstsMaxAge { get; set; } = 31536000;

    public bool HstsIncludeSubdomains { get; set; } = true;

    public bool HstsPreload { get; set; } = true;

    public string ContentSecurityPolicy { get; set; } = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self'; connect-src 'self'; frame-ancestors 'none';";

    public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";

    public string PermissionsPolicy { get; set; } = "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";
}