using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hakeem.Application.Configurations;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Hakeem.Infrastructure.AI.Chat;

internal sealed class ItiChatCompletionService(
    HttpClient httpClient,
    DocumentExtractionAiConfiguration options)
    : IChatCompletionService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition =
                JsonIgnoreCondition.WhenWritingNull
        };

    private readonly Uri _endpoint = options.GetChatEndpointUri();
    private readonly string _apiKey = options.ApiKey;
    private readonly string _modelId = options.ModelId;
    private readonly int _defaultMaxTokens = options.MaxTokens;

    public IReadOnlyDictionary<string, object?> Attributes { get; } =
        new Dictionary<string, object?>
        {
            ["ModelId"] = options.ModelId,
            ["Endpoint"] = options.GetChatEndpointUri()
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
        var request = new ItiChatRequest(
            _modelId,
            chatHistory
                .Where(message => message.Role != AuthorRole.System)
                .Select(message => new ItiChatMessage(
                    GetRole(message.Role),
                    message.Content ?? string.Empty))
                .ToArray(),
            string.IsNullOrWhiteSpace(systemPrompt)
                ? null
                : systemPrompt,
            openAiSettings?.MaxTokens ?? _defaultMaxTokens,
            openAiSettings?.Temperature ?? 0);

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            _endpoint);
        httpRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _apiKey);
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
                $"ITI chat request failed with status {(int)response.StatusCode} " +
                $"({response.StatusCode}). Response: {responseBody}",
                inner: null,
                response.StatusCode);
        }

        ItiChatResponse? result;

        try
        {
            result = JsonSerializer.Deserialize<ItiChatResponse>(
                responseBody,
                JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                "ITI chat returned an invalid JSON response.",
                exception);
        }

        if (string.IsNullOrWhiteSpace(result?.OutputText))
        {
            throw new InvalidDataException(
                "ITI chat returned an empty output_text value.");
        }

        IReadOnlyList<ChatMessageContent> messages =
        [
            new(AuthorRole.Assistant, result.OutputText)
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
            "The ITI chat endpoint does not expose streaming responses.");
    }

    private static string GetRole(AuthorRole role)
    {
        if (role == AuthorRole.Assistant)
        {
            return "assistant";
        }

        if (role == AuthorRole.Tool)
        {
            return "tool";
        }

        return "user";
    }

    private sealed record ItiChatRequest(
        [property: JsonPropertyName("model_id")] string ModelId,
        [property: JsonPropertyName("messages")]
        IReadOnlyList<ItiChatMessage> Messages,
        [property: JsonPropertyName("system_prompt")]
        string? SystemPrompt,
        [property: JsonPropertyName("max_tokens")] int MaxTokens,
        [property: JsonPropertyName("temperature")] double Temperature);

    private sealed record ItiChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ItiChatResponse(
        [property: JsonPropertyName("output_text")] string? OutputText);
}
