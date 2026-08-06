using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hakeem.Application.Configurations;
using Hakeem.Application.Features.MedicalDocuments.DTOs;
using Hakeem.Application.Interfaces.Agents;
using Hakeem.Application.Interfaces.Files;
using Hakeem.Application.Interfaces.Ocr;
using Hakeem.Infrastructure.AI.Agents.Prompts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace Hakeem.Infrastructure.AI.Agents;




/// ///////////////////////////////////////////////// LEGACY CODE /////////////////////////////////////////////////



public sealed class DocumentProcessingWorkflow(
    IDocumentContentProvider contentProvider,
    IDocumentOcrService ocrService,
    IChatCompletionService chatCompletionService,
    IOptions<GeminiChatConfiguration> options,
    ILogger<DocumentProcessingWorkflow> logger)
    : IDocumentProcessingWorkflow
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    private readonly int _maxTokens = options.Value.MaxTokens;

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

        logger.LogInformation(
            "Starting AI processing for document {DocumentId}.",
            documentId);

        await using var document = await contentProvider.OpenReadAsync(
            documentId,
            cancellationToken);

        var ocrResult = await ocrService.ExtractTextAsync(
            document,
            cancellationToken);

        ValidateOcrResult(ocrResult);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(DocumentExtractionPrompt.System);
        chatHistory.AddUserMessage(BuildUserMessage(ocrResult));

        var executionSettings = new GeminiPromptExecutionSettings
        {
            MaxTokens = _maxTokens,
            Temperature = 0,
            ResponseMimeType = "application/json",
            ResponseSchema = typeof(DocumentExtractionResult)
        };

        var response = await chatCompletionService.GetChatMessageContentAsync(
            chatHistory,
            executionSettings,
            cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(response.Content))
        {
            logger.LogWarning(
                "The extraction model returned an empty response for document {DocumentId}.",
                documentId);

            throw new InvalidDataException(
                "The extraction model returned an empty response.");
        }

        DocumentExtractionResult? result;
        var jsonPayload = ExtractJsonPayload(response.Content);

        try
        {
            result = JsonSerializer.Deserialize<DocumentExtractionResult>(
                jsonPayload,
                JsonOptions);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(
                exception,
                "The extraction model returned invalid JSON for document {DocumentId}.",
                documentId);

            throw new InvalidDataException(
                "The extraction model returned invalid JSON.",
                exception);
        }

        var validatedResult = ValidateResponseShape(result);

        logger.LogInformation(
            "AI processing completed for document {DocumentId} with {ItemCount} extracted items.",
            documentId,
            validatedResult.Items.Count);

        return validatedResult;
    }

    private static string ExtractJsonPayload(string content)
    {
        var trimmedContent = content.Trim();
        var objectStart = trimmedContent.IndexOf(
            '{',
            StringComparison.Ordinal);
        var objectEnd = trimmedContent.LastIndexOf(
            '}');

        if (objectStart < 0 || objectEnd < objectStart)
        {
            return trimmedContent;
        }

        return trimmedContent[objectStart..(objectEnd + 1)];
    }

    private static void ValidateOcrResult(OcrResult ocrResult)
    {
        ArgumentNullException.ThrowIfNull(ocrResult);

        if (ocrResult.Pages is null ||
            ocrResult.Pages.Count == 0 ||
            ocrResult.Pages.All(
                page => string.IsNullOrWhiteSpace(page.RecognizedText)))
        {
            throw new InvalidDataException(
                "OCR did not produce any readable document text.");
        }

        if (ocrResult.Pages.Any(page => page.PageNumber <= 0))
        {
            throw new InvalidDataException(
                "OCR returned an invalid page number.");
        }
    }

    private static string BuildUserMessage(OcrResult ocrResult)
    {
        var message = new StringBuilder(
            "Extract structured medical information from the following numbered OCR pages.");

        foreach (var page in ocrResult.Pages)
        {
            message.AppendLine();
            message.AppendLine();
            message.Append("PAGE ");
            message.AppendLine(page.PageNumber.ToString());
            message.Append(page.RecognizedText.Trim());
        }

        message.AppendLine();
        message.AppendLine();
        message.Append("Return only the required JSON object.");

        return message.ToString();
    }

    private static DocumentExtractionResult ValidateResponseShape(
        DocumentExtractionResult? result)
    {
        if (result is null)
        {
            throw new InvalidDataException(
                "The extraction response was empty.");
        }

        if (string.IsNullOrWhiteSpace(result.DocumentType))
        {
            throw new InvalidDataException(
                "The extraction response is missing documentType.");
        }

        if (result.Items is null)
        {
            throw new InvalidDataException(
                "The extraction response items array cannot be null.");
        }

        foreach (var item in result.Items)
        {
            if (item is null ||
                string.IsNullOrWhiteSpace(item.ItemType) ||
                item.SequenceNumber <= 0 ||
                item.PageNumber <= 0 ||
                item.Fields is null)
            {
                throw new InvalidDataException(
                    "The extraction response contains an invalid item.");
            }

            foreach (var field in item.Fields)
            {
                if (field is null ||
                    string.IsNullOrWhiteSpace(field.FieldName) ||
                    field.Issues is null ||
                    field.Confidence is < 0 or > 1)
                {
                    throw new InvalidDataException(
                        "The extraction response contains an invalid field.");
                }
            }
        }

        return result;
    }
}
