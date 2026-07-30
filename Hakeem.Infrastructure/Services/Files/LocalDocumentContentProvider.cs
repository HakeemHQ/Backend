using Hakeem.Application.Interfaces.Files;
using Microsoft.Extensions.Hosting;

namespace Hakeem.Infrastructure.Services.Files;

public sealed class LocalDocumentContentProvider(IHostEnvironment hostEnvironment)
    : IDocumentContentProvider
{
    private const string StorageFolder = "TestDocuments";

    private readonly string _storageRoot = Path.GetFullPath(
        Path.Combine(hostEnvironment.ContentRootPath, StorageFolder));

    public Task<Stream> OpenReadAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (documentId == Guid.Empty)
        {
            throw new ArgumentException(
                "A document ID is required.",
                nameof(documentId));
        }

        if (!Directory.Exists(_storageRoot))
        {
            throw new FileNotFoundException(
                "The document file was not found.");
        }

        var matches = Directory
            .EnumerateFiles(
                _storageRoot,
                $"{documentId:N}.*",
                SearchOption.TopDirectoryOnly)
            .Take(2)
            .ToArray();

        if (matches.Length == 0)
        {
            throw new FileNotFoundException(
                "The document file was not found.");
        }

        if (matches.Length > 1)
        {
            throw new InvalidOperationException(
                "Multiple files were found for the document ID.");
        }

        var relativePath = Path.Combine(
            StorageFolder,
            Path.GetFileName(matches[0]));

        return OpenReadAsync(relativePath, cancellationToken);
    }

    public Task<Stream> OpenReadAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "A document file path is required.",
                nameof(filePath));
        }

        var absolutePath = Path.GetFullPath(
            Path.Combine(hostEnvironment.ContentRootPath, filePath));
        var expectedPrefix = _storageRoot + Path.DirectorySeparatorChar;

        if (!absolutePath.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The document file path is outside the configured storage folder.");
        }

        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException(
                "The document file was not found.",
                filePath);
        }

        Stream stream = new FileStream(
            absolutePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        return Task.FromResult(stream);
    }
}
