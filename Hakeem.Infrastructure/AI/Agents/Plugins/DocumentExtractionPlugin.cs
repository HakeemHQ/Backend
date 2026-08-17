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
    public bool HasUsableDocumentText { get; private set; }
    public MedicalDocumentClassification? AcceptedClassification { get; private set; }
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
        HasUsableDocumentText = ocrResult.Pages.Any(
            page => !string.IsNullOrWhiteSpace(page.RecognizedText));
        _numberedOcrPages = HasUsableDocumentText
            ? BuildNumberedPages(ocrResult)
            : "The OCR service found no readable text in this file.";

        return _numberedOcrPages;
    }

    [KernelFunction("submit_classification")]
    [Description(
        "Validates and submits the medical-relevance classification. " +
        "This must be called after read_document_ocr and before extraction.")]
    public DocumentClassificationSubmission SubmitClassification(
        [Description("The complete medical-document classification result.")]
        MedicalDocumentClassification result)
    {
        if (_numberedOcrPages is null)
        {
            return DocumentClassificationSubmission.Invalid(
                "read_document_ocr must be called before submit_classification.");
        }

        var errors = ValidateClassification(result);
        if (errors.Count > 0)
        {
            LastValidationErrors = errors;
            logger.LogWarning(
                "Classification validation failed for document {DocumentId}: {ValidationErrors}",
                documentId,
                string.Join(" | ", errors));

            return new DocumentClassificationSubmission(false, errors);
        }

        LastValidationErrors = [];
        AcceptedClassification = result;
        return new DocumentClassificationSubmission(true, []);
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

        if (AcceptedClassification?.IsMedical != true)
        {
            return DocumentExtractionSubmission.Invalid(
                "A medical classification must be accepted before submit_extraction.");
        }

        var validationResult = extractionValidator.Validate(result);

        var validationErrors = validationResult.Errors.ToList();
        if (AcceptedClassification.DocumentType != result.DocumentType)
        {
            validationErrors.Add(
                "documentType must match the accepted medical classification.");
        }

        if (validationErrors.Count > 0)
        {
            LastValidationErrors = validationErrors;
            logger.LogWarning(
                "Extraction validation failed for document {DocumentId}: {ValidationErrors}",
                documentId,
                string.Join(" | ", validationErrors));

            return new DocumentExtractionSubmission(
                false,
                validationErrors);
        }

        LastValidationErrors = [];
        AcceptedResult = result;
        return new DocumentExtractionSubmission(true, []);
    }

    private static void ValidateOcrResult(OcrResult ocrResult)
    {
        ArgumentNullException.ThrowIfNull(ocrResult);

        if (ocrResult.Pages is null)
        {
            throw new InvalidDataException(
                "OCR returned a null page collection.");
        }

        if (ocrResult.Pages.Any(page => page.PageNumber <= 0))
        {
            throw new InvalidDataException(
                "OCR returned an invalid page number.");
        }
    }

    private static IReadOnlyList<string> ValidateClassification(
        MedicalDocumentClassification result)
    {
        var errors = new List<string>();

        if (result.Confidence is < 0 or > 1 ||
            double.IsNaN(result.Confidence))
        {
            errors.Add("confidence must be between 0 and 1.");
        }

        if (result.IsMedical)
        {
            if (result.DocumentType is null)
            {
                errors.Add("documentType is required for medical content.");
            }

            if (!string.IsNullOrWhiteSpace(result.RejectionReason))
            {
                errors.Add("rejectionReason must be null for medical content.");
            }
        }
        else
        {
            if (result.DocumentType is not null)
            {
                errors.Add("documentType must be null for non-medical content.");
            }

            if (string.IsNullOrWhiteSpace(result.RejectionReason))
            {
                errors.Add("rejectionReason is required for non-medical content.");
            }
        }

        return errors;
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

public sealed record DocumentClassificationSubmission(
    bool Success,
    IReadOnlyList<string> ValidationErrors)
{
    public static DocumentClassificationSubmission Invalid(string error)
    {
        return new DocumentClassificationSubmission(false, [error]);
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
