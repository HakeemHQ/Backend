namespace Hakeem.Application.Configurations;

public class SmsConfiguration
{
    public const string SectionName = "SMSConfig";

    public string BaseUrl { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
}
