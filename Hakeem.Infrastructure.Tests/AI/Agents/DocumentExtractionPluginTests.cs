using Hakeem.Application.Features.MedicalDocuments.DTOs;
using Hakeem.Application.Interfaces.Ocr;
using Hakeem.Application.Services.DocumentExtraction;
using Hakeem.Infrastructure.AI.Agents.Plugins;
using Hakeem.Infrastructure.Tests.AI.Agents.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hakeem.Infrastructure.Tests.AI.Agents;

public sealed class DocumentExtractionPluginTests
{
    [Fact]
    public async Task ReadDocumentOcrAsync_ReturnsNumberedPages()
    {
        var plugin = CreatePlugin(
            new OcrPageResult(2, "Second page", 0.9m),
            new OcrPageResult(5, "Fifth page", 0.8m));

        var result = await plugin.ReadDocumentOcrAsync(
            CancellationToken.None);

        Assert.Equal(
            "PAGE 2\r\nSecond page\r\n\r\nPAGE 5\r\nFifth page",
            result);
    }

    [Fact]
    public async Task SubmitExtraction_InvalidResult_ReturnsErrorsWithoutAccepting()
    {
        var plugin = CreatePlugin(
            new OcrPageResult(1, "Readable text", 0.9m));
        await plugin.ReadDocumentOcrAsync(CancellationToken.None);
        plugin.SubmitClassification(
            new MedicalDocumentClassification(
                true,
                MedicalDocumentType.Other,
                0.9,
                null));
        var invalid = new DocumentExtractionResult(
            MedicalDocumentType.Other,
            []);

        var submission = plugin.SubmitExtraction(invalid);

        Assert.False(submission.Success);
        Assert.NotEmpty(submission.ValidationErrors);
        Assert.Null(plugin.AcceptedResult);
    }

    [Fact]
    public async Task SubmitExtraction_ValidResult_StoresExactAcceptedResult()
    {
        var plugin = CreatePlugin(
            new OcrPageResult(1, "Readable text", 0.9m));
        await plugin.ReadDocumentOcrAsync(CancellationToken.None);
        plugin.SubmitClassification(
            new MedicalDocumentClassification(
                true,
                MedicalDocumentType.Other,
                0.9,
                null));
        var valid = new DocumentExtractionResult(
            MedicalDocumentType.Other,
            [
                new ExtractedItemResult(
                    "PatientInformation",
                    1,
                    1,
                    [
                        new ExtractedFieldResult(
                            "PatientName",
                            "Example",
                            0.9m,
                            "Example",
                            [])
                    ])
            ]);

        var submission = plugin.SubmitExtraction(valid);

        Assert.True(submission.Success);
        Assert.Empty(submission.ValidationErrors);
        Assert.Same(valid, plugin.AcceptedResult);
    }

    [Fact]
    public async Task SubmitClassification_NonMedicalStructuredResult_IsAccepted()
    {
        var plugin = CreatePlugin(
            new OcrPageResult(1, "Bread, milk, apples", 0.9m));
        await plugin.ReadDocumentOcrAsync(CancellationToken.None);
        var classification = new MedicalDocumentClassification(
            false,
            null,
            0.98,
            "The content is not related to healthcare.");

        var submission = plugin.SubmitClassification(classification);

        Assert.True(submission.Success);
        Assert.Same(classification, plugin.AcceptedClassification);
    }

    [Theory]
    [InlineData(true, null)]
    [InlineData(false, MedicalDocumentType.Prescription)]
    public async Task SubmitClassification_InvalidDocumentTypeShape_ReturnsErrors(
        bool isMedical,
        MedicalDocumentType? documentType)
    {
        var plugin = CreatePlugin(
            new OcrPageResult(1, "Readable text", 0.9m));
        await plugin.ReadDocumentOcrAsync(CancellationToken.None);

        var submission = plugin.SubmitClassification(
            new MedicalDocumentClassification(
                isMedical,
                documentType,
                0.9,
                isMedical ? null : "Not medical."));

        Assert.False(submission.Success);
        Assert.Contains(
            submission.ValidationErrors,
            error => error.Contains("documentType", StringComparison.Ordinal));
        Assert.Null(plugin.AcceptedClassification);
    }

    private static DocumentExtractionPlugin CreatePlugin(
        params OcrPageResult[] pages)
    {
        return new DocumentExtractionPlugin(
            Guid.NewGuid(),
            new FakeDocumentContentProvider(),
            new FakeDocumentOcrService(new OcrResult(pages)),
            new DocumentExtractionValidator(),
            NullLogger<DocumentExtractionPlugin>.Instance);
    }
}
