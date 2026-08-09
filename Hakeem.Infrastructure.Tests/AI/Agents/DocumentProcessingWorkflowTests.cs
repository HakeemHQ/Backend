using System.Net;
using Hakeem.Application.Configurations;
using Hakeem.Application.Features.MedicalDocuments.DTOs;
using Hakeem.Application.Interfaces.Ocr;
using Hakeem.Infrastructure.AI.Agents;
using Hakeem.Infrastructure.Tests.AI.Agents.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace Hakeem.Infrastructure.Tests.AI.Agents;

public sealed class DocumentProcessingWorkflowTests
{
    private const int MaxTokens = 4000;

    [Fact]
    public async Task ProcessAsync_ValidPrescription_ReturnsExtractionResult()
    {
        var contentProvider = new FakeDocumentContentProvider();
        var ocrService = CreateOcrService(
            new OcrPageResult(
                1,
                "Prescription: Amoxicillin 500 mg twice daily.",
                0.98m),
            new OcrPageResult(
                2,
                "Route: oral.",
                0.96m));
        var chatService = new FakeChatCompletionService(
            """
            {
              "documentType": "Prescription",
              "items": [
                {
                  "itemType": "Medication",
                  "sequenceNumber": 1,
                  "pageNumber": 1,
                  "fields": [
                    {
                      "fieldName": "MedicationName",
                      "value": "Amoxicillin",
                      "confidence": 0.98,
                      "evidenceText": "Amoxicillin",
                      "issues": []
                    },
                    {
                      "fieldName": "Dose",
                      "value": "500 mg",
                      "confidence": 0.98,
                      "evidenceText": "500 mg",
                      "issues": []
                    }
                  ]
                }
              ]
            }
            """);
        var workflow = CreateWorkflow(
            contentProvider,
            ocrService,
            chatService);

        var result = await workflow.ProcessAsync(
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Equal(MedicalDocumentType.Prescription, result.DocumentType);
        var item = Assert.Single(result.Items);
        Assert.Equal("Medication", item.ItemType);
        Assert.Equal(2, item.Fields.Count);
        Assert.True(contentProvider.WasCalled);
        Assert.True(ocrService.WasCalled);
        Assert.Equal(1, chatService.CallCount);

        var history = Assert.IsType<ChatHistory>(
            chatService.ReceivedHistory);
        var userMessage = Assert.Single(
            history,
            message => message.Role == AuthorRole.User);
        Assert.Contains("PAGE 1", userMessage.Content);
        Assert.Contains("PAGE 2", userMessage.Content);
        Assert.Contains(
            "Return only the required JSON object.",
            userMessage.Content);

        var settings = Assert.IsType<GeminiPromptExecutionSettings>(
            chatService.ReceivedExecutionSettings);
        Assert.Equal(MaxTokens, settings.MaxTokens);
        Assert.Equal(0, settings.Temperature);
        Assert.Equal(
            typeof(Application.Features.MedicalDocuments.DTOs.DocumentExtractionResult),
            settings.ResponseSchema);
    }

    [Fact]
    public async Task ProcessAsync_EmptyOcr_RejectsBeforeCallingAi()
    {
        var contentProvider = new FakeDocumentContentProvider();
        var ocrService = CreateOcrService(
            new OcrPageResult(1, "   ", null));
        var chatService = new FakeChatCompletionService("{}");
        var workflow = CreateWorkflow(
            contentProvider,
            ocrService,
            chatService);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => workflow.ProcessAsync(
                Guid.NewGuid(),
                CancellationToken.None));

        Assert.Contains("OCR", exception.Message);
        Assert.Equal(0, chatService.CallCount);
    }

    [Fact]
    public async Task ProcessAsync_InvalidModelJson_ThrowsInvalidDataException()
    {
        var chatService = new FakeChatCompletionService(
            "This is not JSON.");
        var workflow = CreateWorkflow(
            new FakeDocumentContentProvider(),
            CreateReadableOcrService(),
            chatService);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => workflow.ProcessAsync(
                Guid.NewGuid(),
                CancellationToken.None));

        Assert.IsType<System.Text.Json.JsonException>(
            exception.InnerException);
    }

    [Fact]
    public async Task ProcessAsync_MissingItems_ThrowsInvalidDataException()
    {
        var chatService = new FakeChatCompletionService(
            """{"documentType":"Prescription"}""");
        var workflow = CreateWorkflow(
            new FakeDocumentContentProvider(),
            CreateReadableOcrService(),
            chatService);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => workflow.ProcessAsync(
                Guid.NewGuid(),
                CancellationToken.None));

        Assert.Contains("items", exception.Message);
    }

    [Fact]
    public async Task ProcessAsync_MissingFields_ThrowsInvalidDataException()
    {
        var chatService = new FakeChatCompletionService(
            """
            {
              "documentType": "Prescription",
              "items": [
                {
                  "itemType": "Medication",
                  "sequenceNumber": 1,
                  "pageNumber": 1
                }
              ]
            }
            """);
        var workflow = CreateWorkflow(
            new FakeDocumentContentProvider(),
            CreateReadableOcrService(),
            chatService);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => workflow.ProcessAsync(
                Guid.NewGuid(),
                CancellationToken.None));

        Assert.Contains("invalid item", exception.Message);
    }

    [Fact]
    public async Task ProcessAsync_Cancelled_PropagatesCancellation()
    {
        var contentProvider = new FakeDocumentContentProvider();
        var ocrService = CreateReadableOcrService();
        var chatService = new FakeChatCompletionService("{}");
        var workflow = CreateWorkflow(
            contentProvider,
            ocrService,
            chatService);
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => workflow.ProcessAsync(
                Guid.NewGuid(),
                cancellationSource.Token));

        Assert.False(contentProvider.WasCalled);
        Assert.False(ocrService.WasCalled);
        Assert.Equal(0, chatService.CallCount);
    }

    [Fact]
    public async Task ProcessAsync_TemporaryAiFailure_PropagatesFailure()
    {
        var expectedException = new HttpRequestException(
            "The AI service is temporarily unavailable.",
            inner: null,
            HttpStatusCode.ServiceUnavailable);
        var chatService = new FakeChatCompletionService(
            expectedException);
        var workflow = CreateWorkflow(
            new FakeDocumentContentProvider(),
            CreateReadableOcrService(),
            chatService);

        var actualException = await Assert.ThrowsAsync<HttpRequestException>(
            () => workflow.ProcessAsync(
                Guid.NewGuid(),
                CancellationToken.None));

        Assert.Same(expectedException, actualException);
        Assert.Equal(
            HttpStatusCode.ServiceUnavailable,
            actualException.StatusCode);
    }

    private static DocumentProcessingWorkflow CreateWorkflow(
        FakeDocumentContentProvider contentProvider,
        FakeDocumentOcrService ocrService,
        FakeChatCompletionService chatService)
    {
        var options = Options.Create(
            new GeminiChatConfiguration
            {
                MaxTokens = MaxTokens
            });

        return new DocumentProcessingWorkflow(
            contentProvider,
            ocrService,
            chatService,
            options,
            NullLogger<DocumentProcessingWorkflow>.Instance);
    }

    private static FakeDocumentOcrService CreateReadableOcrService()
    {
        return CreateOcrService(
            new OcrPageResult(
                1,
                "Prescription: Amoxicillin 500 mg.",
                0.98m));
    }

    private static FakeDocumentOcrService CreateOcrService(
        params OcrPageResult[] pages)
    {
        return new FakeDocumentOcrService(
            new OcrResult(pages));
    }
}
