namespace Hakeem.Application.Configurations;

public sealed class GeminiChatConfiguration
{
    public const string SectionName = "GeminiChat";

    public string ModelId { get; set; } = string.Empty;
    public int MaxTokens { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public int MaxAgentIterations { get; set; } = 4;
    public int AgentTimeoutSeconds { get; set; } = 120;
}
