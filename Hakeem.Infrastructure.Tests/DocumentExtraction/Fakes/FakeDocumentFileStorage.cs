using Hakeem.Application.Interfaces.Files;
using Microsoft.AspNetCore.Http;

namespace Hakeem.Infrastructure.Tests.DocumentExtraction.Fakes;

internal sealed class FakeDocumentFileStorage : IDocumentFileStorage
{
    public int DeleteCallCount { get; private set; }
    public string? DeletedFilePath { get; private set; }
    public Exception? DeleteException { get; init; }

    public Task<string> SaveAsync(
        IFormFile file,
        Guid documentId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<bool> DeleteAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DeleteCallCount++;
        DeletedFilePath = filePath;

        if (DeleteException is not null)
        {
            return Task.FromException<bool>(DeleteException);
        }

        return Task.FromResult(true);
    }
}
