namespace Hakeem.Application.Configurations;

public class JwtConfiguration
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int TokenExpirationMinutes { get; set; } = 120;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
