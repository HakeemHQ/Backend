namespace Hakeem.Application.Configurations;

public sealed class GeminiChatConfiguration
{
    public const string SectionName = "GeminiChat";

    public string BaseUrl { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public int MaxTokens { get; set; }
    public string ApiKey { get; set; } = string.Empty;

    public Uri GetGenerateContentEndpointUri()
    {
        var normalizedBaseUrl = BaseUrl.EndsWith(
            "/",
            StringComparison.Ordinal)
            ? BaseUrl
            : $"{BaseUrl}/";
        var modelId = Uri.EscapeDataString(ModelId);

        return new Uri(
            new Uri(normalizedBaseUrl, UriKind.Absolute),
            $"v1beta/models/{modelId}:generateContent");
    }
}
