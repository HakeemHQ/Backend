using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hakeem.Application.Configurations;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Hakeem.Infrastructure.AI.Chat;

internal sealed class GeminiChatCompletionService(
    HttpClient httpClient,
    GeminiChatConfiguration options)
    : IChatCompletionService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition =
                JsonIgnoreCondition.WhenWritingNull
        };

    private readonly Uri _endpoint =
        options.GetGenerateContentEndpointUri();
    private readonly string _apiKey = options.ApiKey;
    private readonly string _modelId = options.ModelId;
    private readonly int _defaultMaxTokens = options.MaxTokens;

    public IReadOnlyDictionary<string, object?> Attributes { get; } =
        new Dictionary<string, object?>
        {
            ["ModelId"] = options.ModelId,
            ["Endpoint"] = options.GetGenerateContentEndpointUri()
        };

    public async Task<IReadOnlyList<ChatMessageContent>>
        GetChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chatHistory);

        var openAiSettings =
            executionSettings as OpenAIPromptExecutionSettings;
        var systemPrompt = string.Join(
            Environment.NewLine + Environment.NewLine,
            chatHistory
                .Where(message => message.Role == AuthorRole.System)
                .Select(message => message.Content)
                .Where(content => !string.IsNullOrWhiteSpace(content)));
        var request = new GeminiGenerateContentRequest(
            chatHistory
                .Where(message => message.Role != AuthorRole.System)
                .Select(message => new GeminiContent(
                    GetRole(message.Role),
                    [new GeminiPart(message.Content ?? string.Empty)]))
                .ToArray(),
            string.IsNullOrWhiteSpace(systemPrompt)
                ? null
                : new GeminiContent(
                    Role: null,
                    [new GeminiPart(systemPrompt)]),
            new GeminiGenerationConfig(
                openAiSettings?.MaxTokens ?? _defaultMaxTokens,
                openAiSettings?.ResponseFormat is null
                    ? null
                    : "application/json"));

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            _endpoint);
        httpRequest.Headers.Add("x-goog-api-key", _apiKey);
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(request, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Gemini chat request failed with status {(int)response.StatusCode} " +
                $"({response.StatusCode}). Response: {responseBody}",
                inner: null,
                response.StatusCode);
        }

        GeminiGenerateContentResponse? result;

        try
        {
            result = JsonSerializer.Deserialize<
                GeminiGenerateContentResponse>(
                    responseBody,
                    JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                "Gemini chat returned an invalid JSON response.",
                exception);
        }

        var outputText = result?.Candidates?
            .FirstOrDefault()?
            .Content?
            .Parts?
            .Where(part => !string.IsNullOrEmpty(part.Text))
            .Select(part => part.Text)
            .Aggregate(
                new StringBuilder(),
                (builder, text) => builder.Append(text))
            .ToString();

        if (string.IsNullOrWhiteSpace(outputText))
        {
            throw new InvalidDataException(
                "Gemini chat returned an empty response.");
        }

        IReadOnlyList<ChatMessageContent> messages =
        [
            new(AuthorRole.Assistant, outputText, _modelId)
        ];

        return messages;
    }

    public IAsyncEnumerable<StreamingChatMessageContent>
        GetStreamingChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "The Gemini chat service does not expose streaming responses.");
    }

    private static string GetRole(AuthorRole role)
    {
        return role == AuthorRole.Assistant
            ? "model"
            : "user";
    }

    private sealed record GeminiGenerateContentRequest(
        IReadOnlyList<GeminiContent> Contents,
        GeminiContent? SystemInstruction,
        GeminiGenerationConfig GenerationConfig);

    private sealed record GeminiContent(
        string? Role,
        IReadOnlyList<GeminiPart> Parts);

    private sealed record GeminiPart(string? Text);

    private sealed record GeminiGenerationConfig(
        int MaxOutputTokens,
        string? ResponseMimeType);

    private sealed record GeminiGenerateContentResponse(
        IReadOnlyList<GeminiCandidate>? Candidates);

    private sealed record GeminiCandidate(GeminiContent? Content);
}
