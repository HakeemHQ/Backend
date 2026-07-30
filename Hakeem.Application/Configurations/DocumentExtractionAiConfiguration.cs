namespace Hakeem.Application.Configurations;

public sealed class DocumentExtractionAiConfiguration
{
    public const string SectionName = "DocumentExtractionAi";
    public const string ServiceId = "DocumentExtractionAi";

    public string BaseUrl { get; set; } = string.Empty;
    public string ChatEndpoint { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public int MaxTokens { get; set; }
    public string ApiKey { get; set; } = string.Empty;

    public Uri GetChatEndpointUri()
    {
        var normalizedBaseUrl = BaseUrl.EndsWith(
            "/",
            StringComparison.Ordinal)
            ? BaseUrl
            : $"{BaseUrl}/";

        return new Uri(
            new Uri(normalizedBaseUrl, UriKind.Absolute),
            ChatEndpoint.TrimStart('/'));
    }
}
