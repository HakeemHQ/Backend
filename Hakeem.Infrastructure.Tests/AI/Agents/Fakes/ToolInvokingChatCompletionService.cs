using Hakeem.Application.Features.MedicalDocuments.DTOs;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Hakeem.Infrastructure.Tests.AI.Agents.Fakes;

internal sealed class ToolInvokingChatCompletionService
    : IChatCompletionService
{
    private readonly MedicalDocumentClassification _classification;
    private readonly IReadOnlyList<DocumentExtractionResult>
        _submittedResults;
    private readonly string _finalText;

    public ToolInvokingChatCompletionService(
        DocumentExtractionResult submittedResult,
        string finalText)
        : this(
            new MedicalDocumentClassification(
                true,
                submittedResult.DocumentType,
                0.99,
                null),
            [submittedResult],
            finalText)
    {
    }

    public ToolInvokingChatCompletionService(
        IReadOnlyList<DocumentExtractionResult> submittedResults,
        string finalText)
        : this(
            new MedicalDocumentClassification(
                true,
                submittedResults[0].DocumentType,
                0.99,
                null),
            submittedResults,
            finalText)
    {
    }

    public ToolInvokingChatCompletionService(
        MedicalDocumentClassification classification,
        IReadOnlyList<DocumentExtractionResult> submittedResults,
        string finalText)
    {
        _classification = classification;
        _submittedResults = submittedResults;
        _finalText = finalText;
    }

    public IReadOnlyDictionary<string, object?> Attributes { get; } =
        new Dictionary<string, object?>();

    public int CallCount { get; private set; }
    public List<PromptExecutionSettings?> ReceivedExecutionSettings
        { get; } = [];
    public List<IReadOnlyList<string>> AvailableFunctionNames
        { get; } = [];

    public async Task<IReadOnlyList<ChatMessageContent>>
        GetChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(kernel);

        CallCount++;
        ReceivedExecutionSettings.Add(executionSettings);

        var plugin = kernel.Plugins["documentextraction"];
        AvailableFunctionNames.Add(
            plugin.Select(function => function.Name).ToArray());

        if (CallCount == 1)
        {
            await plugin["read_document_ocr"].InvokeAsync(
                kernel,
                cancellationToken: cancellationToken);
        }
        else if (CallCount == 2)
        {
            await plugin["submit_classification"].InvokeAsync(
                kernel,
                new KernelArguments
                {
                    ["result"] = _classification
                },
                cancellationToken);
        }
        else
        {
            var submissionIndex = Math.Min(
                CallCount - 3,
                _submittedResults.Count - 1);

            await plugin["submit_extraction"].InvokeAsync(
                kernel,
                new KernelArguments
                {
                    ["result"] = _submittedResults[submissionIndex]
                },
                cancellationToken);
        }

        return
        [
            new ChatMessageContent(AuthorRole.Assistant, _finalText)
        ];
    }

    public IAsyncEnumerable<StreamingChatMessageContent>
        GetStreamingChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }
}
