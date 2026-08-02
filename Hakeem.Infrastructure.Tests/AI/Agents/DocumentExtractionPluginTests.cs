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
        var invalid = new DocumentExtractionResult(
            "UnsupportedType",
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
        var valid = new DocumentExtractionResult("Other", []);

        var submission = plugin.SubmitExtraction(valid);

        Assert.True(submission.Success);
        Assert.Empty(submission.ValidationErrors);
        Assert.Same(valid, plugin.AcceptedResult);
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
