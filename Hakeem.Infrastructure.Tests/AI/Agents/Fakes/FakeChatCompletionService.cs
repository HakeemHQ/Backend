using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Hakeem.Infrastructure.Tests.AI.Agents.Fakes;

internal sealed class FakeChatCompletionService
    : IChatCompletionService
{
    private readonly string? _responseContent;
    private readonly Exception? _exception;

    public FakeChatCompletionService(string responseContent)
    {
        _responseContent = responseContent;
    }

    public FakeChatCompletionService(Exception exception)
    {
        _exception = exception;
    }

    public IReadOnlyDictionary<string, object?> Attributes { get; } =
        new Dictionary<string, object?>();

    public int CallCount { get; private set; }
    public ChatHistory? ReceivedHistory { get; private set; }
    public PromptExecutionSettings? ReceivedExecutionSettings { get; private set; }

    public Task<IReadOnlyList<ChatMessageContent>>
        GetChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CallCount++;
        ReceivedHistory = chatHistory;
        ReceivedExecutionSettings = executionSettings;

        if (_exception is not null)
        {
            return Task.FromException<
                IReadOnlyList<ChatMessageContent>>(_exception);
        }

        IReadOnlyList<ChatMessageContent> messages =
        [
            new(AuthorRole.Assistant, _responseContent)
        ];

        return Task.FromResult(messages);
    }

    public IAsyncEnumerable<StreamingChatMessageContent>
        GetStreamingChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "Streaming is not used by DocumentProcessingAgent tests.");
    }
}
