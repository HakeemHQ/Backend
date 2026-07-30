using Hakeem.Application.Interfaces.Files;

namespace Hakeem.Infrastructure.Tests.AI.Agents.Fakes;

internal sealed class FakeDocumentContentProvider(
    byte[]? content = null)
    : IDocumentContentProvider
{
    private readonly byte[] _content = content ?? [1, 2, 3];

    public bool WasCalled { get; private set; }

    public Task<Stream> OpenReadAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        WasCalled = true;

        Stream stream = new MemoryStream(
            _content,
            writable: false);

        return Task.FromResult(stream);
    }

    public Task<Stream> OpenReadAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        WasCalled = true;

        Stream stream = new MemoryStream(
            _content,
            writable: false);

        return Task.FromResult(stream);
    }
}
