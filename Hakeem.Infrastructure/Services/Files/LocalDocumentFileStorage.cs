using System.Text;
using Hakeem.Application.Configurations;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Interfaces.Files;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Hakeem.Infrastructure.Services.Files;

public sealed class LocalDocumentFileStorage(
    IHostEnvironment hostEnvironment,
    IOptions<FileStorageConfiguration> fileStorageOptions)
    : IDocumentFileStorage
{
    private const string StorageFolder = "TestDocuments";

    private static readonly IReadOnlyDictionary<string, string> ContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".gif"] = "image/gif",
            [".bmp"] = "image/bmp",
            [".webp"] = "image/webp"
        };

    private readonly string _storageRoot = Path.GetFullPath(
        Path.Combine(hostEnvironment.ContentRootPath, StorageFolder));
    private readonly long _maxFileSizeBytes = fileStorageOptions.Value.MaxFileSizeBytes;

    public async Task<string> SaveAsync(
        IFormFile file,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        if (file.Length > _maxFileSizeBytes)
        {
            throw new PayloadTooLargeException(
                ErrorCodes.DocumentFileTooLarge,
                _maxFileSizeBytes);
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!ContentTypes.TryGetValue(extension, out var expectedContentType) ||
            !string.Equals(file.ContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnsupportedMediaTypeException(
                ErrorCodes.DocumentUnsupportedMediaType);
        }

        if (!await HasValidSignatureAsync(file, extension, cancellationToken))
        {
            throw new UnsupportedMediaTypeException(
                ErrorCodes.DocumentInvalidFileContent);
        }

        Directory.CreateDirectory(_storageRoot);

        var fileName = $"{documentId:N}{extension}";
        var absolutePath = Path.Combine(_storageRoot, fileName);

        try
        {
            await using var destination = new FileStream(
                absolutePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);
            await file.CopyToAsync(destination, cancellationToken);
        }
        catch
        {
            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }

            throw;
        }

        return $"{StorageFolder}/{fileName}";
    }

    public Task<bool> DeleteAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var absolutePath = Path.GetFullPath(
            Path.Combine(hostEnvironment.ContentRootPath, filePath));
        var expectedPrefix = _storageRoot + Path.DirectorySeparatorChar;

        if (!absolutePath.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The document file path is outside the configured storage folder.");
        }

        if (!File.Exists(absolutePath))
        {
            return Task.FromResult(false);
        }

        File.Delete(absolutePath);
        return Task.FromResult(true);
    }

    private static async Task<bool> HasValidSignatureAsync(
        IFormFile file,
        string extension,
        CancellationToken cancellationToken)
    {
        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAsync(header, cancellationToken);

        return extension switch
        {
            ".pdf" => StartsWith(header, bytesRead, "%PDF-"u8),
            ".jpg" or ".jpeg" =>
                bytesRead >= 3 &&
                header[0] == 0xFF &&
                header[1] == 0xD8 &&
                header[2] == 0xFF,
            ".png" => StartsWith(
                header,
                bytesRead,
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".gif" =>
                StartsWith(header, bytesRead, Encoding.ASCII.GetBytes("GIF87a")) ||
                StartsWith(header, bytesRead, Encoding.ASCII.GetBytes("GIF89a")),
            ".bmp" => StartsWith(header, bytesRead, "BM"u8),
            ".webp" =>
                StartsWith(header, bytesRead, "RIFF"u8) &&
                bytesRead >= 12 &&
                header.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            _ => false
        };
    }

    private static bool StartsWith(
        byte[] source,
        int sourceLength,
        ReadOnlySpan<byte> signature)
    {
        return sourceLength >= signature.Length &&
               source.AsSpan(0, signature.Length).SequenceEqual(signature);
    }
}
