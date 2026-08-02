using Hakeem.Application.Configurations;
using Hakeem.Application.Features.MedicalDocuments.DTOs;
using Hakeem.Application.Interfaces.Agents;
using Hakeem.Application.Interfaces.Files;
using Hakeem.Application.Interfaces.Ocr;
using Hakeem.Application.Interfaces.Validation;
using Hakeem.Infrastructure.AI.Agents.Plugins;
using Hakeem.Infrastructure.AI.Agents.Prompts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace Hakeem.Infrastructure.AI.Agents;

public sealed class DocumentProcessingAgent(
    IDocumentContentProvider contentProvider,
    IDocumentOcrService ocrService,
    IDocumentExtractionValidator extractionValidator,
    IChatCompletionService chatCompletionService,
    IOptions<GeminiChatConfiguration> options,
    ILoggerFactory loggerFactory,
    ILogger<DocumentProcessingAgent> logger)
    : IDocumentProcessingAgent
{
    private const string ExtractionPluginName = "documentextraction";

    private readonly GeminiChatConfiguration _configuration =
        options.Value;

    public async Task<DocumentExtractionResult> ProcessAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        if (documentId == Guid.Empty)
        {
            throw new ArgumentException(
                "A document ID is required.",
                nameof(documentId));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var plugin = new DocumentExtractionPlugin(
            documentId,
            contentProvider,
            ocrService,
            extractionValidator,
            loggerFactory.CreateLogger<DocumentExtractionPlugin>());

        var invocationFilter = new BoundedToolInvocationFilter(
            _configuration.MaxAgentIterations,
            logger);
        var extractionPlugin = KernelPluginFactory.CreateFromObject(
            plugin,
            ExtractionPluginName);
        var readOcrKernel = CreatePhaseKernel(
            chatCompletionService,
            extractionPlugin["read_document_ocr"],
            invocationFilter);
        var submitKernel = CreatePhaseKernel(
            chatCompletionService,
            extractionPlugin["submit_extraction"],
            invocationFilter);
        var readOcrAgent = CreateAgent(
            readOcrKernel,
            readOcrKernel.Plugins[ExtractionPluginName]
                ["read_document_ocr"]);
        var submitAgent = CreateAgent(
            submitKernel,
            submitKernel.Plugins[ExtractionPluginName]
                ["submit_extraction"]);

        using var timeoutSource = new CancellationTokenSource(
            TimeSpan.FromSeconds(
                _configuration.AgentTimeoutSeconds));
        using var linkedSource = CancellationTokenSource
            .CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token);

        logger.LogInformation(
            "Starting bounded extraction agent for document {DocumentId}.",
            documentId);

        try
        {
            AgentThread? thread = null;

            thread = await InvokeAgentAsync(
                readOcrAgent,
                DocumentExtractionAgentPrompt.ReadOcr,
                thread,
                linkedSource.Token);

            if (!plugin.HasReadDocumentOcr)
            {
                throw new InvalidDataException(
                    "The extraction agent did not call read_document_ocr.");
            }

            var maximumSubmissionAttempts =
                _configuration.MaxAgentIterations - 1;

            for (var attempt = 1;
                 attempt <= maximumSubmissionAttempts &&
                 plugin.AcceptedResult is null;
                 attempt++)
            {
                var prompt = attempt == 1
                    ? DocumentExtractionAgentPrompt.Submit
                    : DocumentExtractionAgentPrompt.Repair;

                thread = await InvokeAgentAsync(
                    submitAgent,
                    prompt,
                    thread,
                    linkedSource.Token);
            }
        }
        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested &&
                  timeoutSource.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Document extraction exceeded the {_configuration.AgentTimeoutSeconds}-second timeout.",
                exception);
        }

        var acceptedResult = plugin.AcceptedResult
            ?? throw new InvalidDataException(
                "The extraction agent finished without submitting a valid result.");

        logger.LogInformation(
            "Bounded extraction agent completed for document {DocumentId} with {ItemCount} items.",
            documentId,
            acceptedResult.Items.Count);

        return acceptedResult;
    }

    private static Kernel CreatePhaseKernel(
        IChatCompletionService chatCompletionService,
        KernelFunction phaseFunction,
        BoundedToolInvocationFilter invocationFilter)
    {
        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.Services.AddSingleton(chatCompletionService);
        var kernel = kernelBuilder.Build();
        kernel.Plugins.AddFromFunctions(
            ExtractionPluginName,
            [phaseFunction]);
        kernel.AutoFunctionInvocationFilters.Add(invocationFilter);

        return kernel;
    }

    private ChatCompletionAgent CreateAgent(
        Kernel kernel,
        KernelFunction requiredFunction)
    {
        var executionSettings = new GeminiPromptExecutionSettings
        {
            MaxTokens = _configuration.MaxTokens,
            Temperature = 0,
#pragma warning disable CS0618
            ToolCallBehavior =
                GeminiToolCallBehavior.AutoInvokeKernelFunctions,
#pragma warning restore CS0618
            FunctionChoiceBehavior = FunctionChoiceBehavior.Required(
                [requiredFunction],
                autoInvoke: true,
                new FunctionChoiceBehaviorOptions
                {
                    AllowConcurrentInvocation = false,
                    AllowParallelCalls = false,
                    AllowStrictSchemaAdherence = true
                })
        };

        return new ChatCompletionAgent
        {
            Name = "DocumentExtractionAgent",
            Instructions = DocumentExtractionAgentPrompt.System,
            Kernel = kernel,
            Arguments = new KernelArguments(executionSettings)
        };
    }

    private static async Task<AgentThread?> InvokeAgentAsync(
        ChatCompletionAgent agent,
        string prompt,
        AgentThread? thread,
        CancellationToken cancellationToken)
    {
        await foreach (var response in agent.InvokeAsync(
                           prompt,
                           thread,
                           cancellationToken: cancellationToken))
        {
            // Preserve tool results and validation errors on the thread,
            // while deliberately ignoring arbitrary model text.
            thread = response.Thread;
        }

        return thread;
    }
}
