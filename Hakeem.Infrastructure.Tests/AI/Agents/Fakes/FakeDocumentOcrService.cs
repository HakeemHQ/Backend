using Hakeem.Application.Interfaces.Ocr;

namespace Hakeem.Infrastructure.Tests.AI.Agents.Fakes;

internal sealed class FakeDocumentOcrService(OcrResult result)
    : IDocumentOcrService
{
    public bool WasCalled { get; private set; }

    public Task<OcrResult> ExtractTextAsync(
        Stream document,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        WasCalled = true;

        return Task.FromResult(result);
    }
}
