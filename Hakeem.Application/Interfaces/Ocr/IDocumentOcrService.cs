using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Interfaces.Ocr;

public interface IDocumentOcrService : IScoped
{
    Task<OcrResult> ExtractTextAsync(
        Stream document,
        CancellationToken cancellationToken);
}
