namespace Hakeem.Application.Configurations;

public class EmailSettingsConfiguration
{
    public const string SectionName = "EmailConfig";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
