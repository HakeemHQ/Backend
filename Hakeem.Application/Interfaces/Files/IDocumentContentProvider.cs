using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Interfaces.Files;

public interface IDocumentContentProvider : IScoped
{
    Task<Stream> OpenReadAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}
