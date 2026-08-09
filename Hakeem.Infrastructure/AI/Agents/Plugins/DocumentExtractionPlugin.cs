using System.ComponentModel;
using System.Text;
using Hakeem.Application.Features.MedicalDocuments.DTOs;
using Hakeem.Application.Interfaces.Files;
using Hakeem.Application.Interfaces.Ocr;
using Hakeem.Application.Interfaces.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Hakeem.Infrastructure.AI.Agents.Plugins;

public sealed class DocumentExtractionPlugin(
    Guid documentId,
    IDocumentContentProvider contentProvider,
    IDocumentOcrService ocrService,
    IDocumentExtractionValidator extractionValidator,
    ILogger<DocumentExtractionPlugin> logger)
{
    private string? _numberedOcrPages;

    public bool HasReadDocumentOcr => _numberedOcrPages is not null;
    public DocumentExtractionResult? AcceptedResult { get; private set; }
    public IReadOnlyList<string> LastValidationErrors { get; private set; } = [];

    [KernelFunction("read_document_ocr")]
    [Description(
        "Reads the current document with OCR and returns its numbered pages. " +
        "This must be called before submitting an extraction.")]
    public async Task<string> ReadDocumentOcrAsync(
        CancellationToken cancellationToken)
    {
        if (_numberedOcrPages is not null)
        {
            return _numberedOcrPages;
        }

        await using var document = await contentProvider.OpenReadAsync(
            documentId,
            cancellationToken);

        var ocrResult = await ocrService.ExtractTextAsync(
            document,
            cancellationToken);

        ValidateOcrResult(ocrResult);
        _numberedOcrPages = BuildNumberedPages(ocrResult);

        return _numberedOcrPages;
    }

    [KernelFunction("submit_extraction")]
    [Description(
        "Validates and submits the candidate structured extraction. " +
        "If validation errors are returned, repair the candidate and retry.")]
    public DocumentExtractionSubmission SubmitExtraction(
        [Description("The complete candidate document extraction result.")]
        DocumentExtractionResult result)
    {
        if (_numberedOcrPages is null)
        {
            return DocumentExtractionSubmission.Invalid(
                "read_document_ocr must be called before submit_extraction.");
        }

        var validationResult = extractionValidator.Validate(result);

        if (!validationResult.IsValid)
        {
            LastValidationErrors = validationResult.Errors;
            logger.LogWarning(
                "Extraction validation failed for document {DocumentId}: {ValidationErrors}",
                documentId,
                string.Join(" | ", validationResult.Errors));

            return new DocumentExtractionSubmission(
                false,
                validationResult.Errors);
        }

        LastValidationErrors = [];
        AcceptedResult = result;
        return new DocumentExtractionSubmission(true, []);
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

    private static string BuildNumberedPages(OcrResult ocrResult)
    {
        var pages = new StringBuilder();

        foreach (var page in ocrResult.Pages)
        {
            if (pages.Length > 0)
            {
                pages.AppendLine();
                pages.AppendLine();
            }

            pages.Append("PAGE ");
            pages.AppendLine(page.PageNumber.ToString());
            pages.Append(page.RecognizedText.Trim());
        }

        return pages.ToString();
    }
}

public sealed record DocumentExtractionSubmission(
    bool Success,
    IReadOnlyList<string> ValidationErrors)
{
    public static DocumentExtractionSubmission Invalid(string error)
    {
        return new DocumentExtractionSubmission(false, [error]);
    }
}
