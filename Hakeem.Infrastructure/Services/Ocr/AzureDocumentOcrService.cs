using Azure;
using Azure.AI.DocumentIntelligence;
using Hakeem.Application.Configurations;
using Hakeem.Application.Interfaces.Ocr;
using Microsoft.Extensions.Options;

namespace Hakeem.Infrastructure.Services.Ocr;

public sealed class AzureDocumentOcrService(
    DocumentIntelligenceClient client,
    IOptions<AzureDocumentIntelligenceOptions> options)
    : IDocumentOcrService
{
    private readonly string _modelId = options.Value.ModelId;

    public async Task<OcrResult> ExtractTextAsync(
        Stream document,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (!document.CanRead)
        {
            throw new ArgumentException(
                "The document stream must be readable.",
                nameof(document));
        }

        var documentContent = await BinaryData.FromStreamAsync(
            document,
            cancellationToken);
        var analyzeOptions = new AnalyzeDocumentOptions(
            _modelId,
            documentContent);

        var operation = await client.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            analyzeOptions,
            cancellationToken);

        var pages = operation.Value.Pages
            .Select(page => new OcrPageResult(
                page.PageNumber,
                string.Join(
                    Environment.NewLine,
                    page.Lines.Select(line => line.Content)),
                CalculatePageConfidence(page)))
            .ToList();

        return new OcrResult(pages);
    }

    private static decimal? CalculatePageConfidence(DocumentPage page)
    {
        if (page.Words.Count == 0)
        {
            return null;
        }

        var totalConfidence = page.Words.Sum(
            word => (decimal)word.Confidence);

        return decimal.Round(
            totalConfidence / page.Words.Count,
            decimals: 4,
            MidpointRounding.AwayFromZero);
    }
}
