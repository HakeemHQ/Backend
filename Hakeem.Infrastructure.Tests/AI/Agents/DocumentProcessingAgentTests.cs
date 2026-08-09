using Hakeem.Application.Configurations;
using Hakeem.Application.Features.MedicalDocuments.DTOs;
using Hakeem.Application.Services.DocumentExtraction;
using Hakeem.Infrastructure.AI.Agents;
using Hakeem.Infrastructure.Tests.AI.Agents.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;

namespace Hakeem.Infrastructure.Tests.AI.Agents;

public sealed class DocumentProcessingAgentTests
{
    [Fact]
    public async Task ProcessAsync_ReturnsAcceptedSubmissionAndIgnoresFinalText()
    {
        var expected = CreateValidResult("MedicationName");
        var contentProvider = new FakeDocumentContentProvider();
        var ocrService = CreateReadableOcrService();
        var chatService = new ToolInvokingChatCompletionService(
            expected,
            "Arbitrary model text that must not be parsed or returned.");
        var agent = CreateAgent(
            contentProvider,
            ocrService,
            chatService);

        var actual = await agent.ProcessAsync(
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Same(expected, actual);
        Assert.True(contentProvider.WasCalled);
        Assert.True(ocrService.WasCalled);
        Assert.Equal(2, chatService.CallCount);
        Assert.Equal(
            ["read_document_ocr"],
            chatService.AvailableFunctionNames[0]);
        Assert.Equal(
            ["submit_extraction"],
            chatService.AvailableFunctionNames[1]);
        Assert.All(
            chatService.ReceivedExecutionSettings,
            settings =>
            {
                var geminiSettings =
                    Assert.IsType<GeminiPromptExecutionSettings>(settings);

                Assert.IsType<RequiredFunctionChoiceBehavior>(
                    geminiSettings.FunctionChoiceBehavior);
#pragma warning disable CS0618
                Assert.NotNull(geminiSettings.ToolCallBehavior);
                Assert.True(
                    geminiSettings.ToolCallBehavior
                        .MaximumAutoInvokeAttempts > 0);
#pragma warning restore CS0618
            });
    }

    [Fact]
    public async Task ProcessAsync_InvalidSubmission_RepairsOnSameWorkflow()
    {
        var invalid = CreateValidResult("UnsupportedField");
        var expected = CreateValidResult("MedicationName");
        var chatService = new ToolInvokingChatCompletionService(
            [invalid, expected],
            "Arbitrary final text.");
        var agent = CreateAgent(
            new FakeDocumentContentProvider(),
            CreateReadableOcrService(),
            chatService);

        var actual = await agent.ProcessAsync(
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Same(expected, actual);
        Assert.Equal(3, chatService.CallCount);
    }

    [Fact]
    public async Task ProcessAsync_RequiredOcrToolNotCalled_Throws()
    {
        var agent = CreateAgent(
            new FakeDocumentContentProvider(),
            CreateReadableOcrService(),
            new FakeChatCompletionService(
                "Finished without using the extraction tools."));

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => agent.ProcessAsync(
                Guid.NewGuid(),
                CancellationToken.None));

        Assert.Contains("did not call read_document_ocr", exception.Message);
    }

    [Fact]
    public async Task ProcessAsync_CancelledBeforeInvocation_PropagatesCancellation()
    {
        var contentProvider = new FakeDocumentContentProvider();
        var ocrService = CreateReadableOcrService();
        var agent = CreateAgent(
            contentProvider,
            ocrService,
            new FakeChatCompletionService("unused"));
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => agent.ProcessAsync(
                Guid.NewGuid(),
                cancellationSource.Token));

        Assert.False(contentProvider.WasCalled);
        Assert.False(ocrService.WasCalled);
    }

    private static DocumentProcessingAgent CreateAgent(
        FakeDocumentContentProvider contentProvider,
        FakeDocumentOcrService ocrService,
        Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService
            chatService)
    {
        return new DocumentProcessingAgent(
            contentProvider,
            ocrService,
            new DocumentExtractionValidator(),
            chatService,
            Options.Create(
                new GeminiChatConfiguration
                {
                    MaxTokens = 4000,
                    MaxAgentIterations = 4,
                    AgentTimeoutSeconds = 30
                }),
            NullLoggerFactory.Instance,
            NullLogger<DocumentProcessingAgent>.Instance);
    }

    private static FakeDocumentOcrService CreateReadableOcrService()
    {
        return new FakeDocumentOcrService(
            new Application.Interfaces.Ocr.OcrResult(
                [
                    new Application.Interfaces.Ocr.OcrPageResult(
                        1,
                        "Prescription: Amoxicillin 500 mg.",
                        0.98m)
                ]));
    }

    private static DocumentExtractionResult CreateValidResult(
        string fieldName)
    {
        return new DocumentExtractionResult(
            MedicalDocumentType.Prescription,
            [
                new ExtractedItemResult(
                    "Medication",
                    1,
                    1,
                    [
                        new ExtractedFieldResult(
                            fieldName,
                            "Amoxicillin",
                            0.98m,
                            "Amoxicillin",
                            [])
                    ])
            ]);
    }
}
