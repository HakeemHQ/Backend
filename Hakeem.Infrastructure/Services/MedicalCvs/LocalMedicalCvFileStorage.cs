using Hakeem.Application.Interfaces.MedicalCvs;
using Hakeem.Domain.Interfaces.ServiceLifetime;
using Microsoft.Extensions.Hosting;

namespace Hakeem.Infrastructure.Services.MedicalCvs;

public sealed class LocalMedicalCvFileStorage(IHostEnvironment hostEnvironment)
    : IMedicalCvFileStorage, IScoped
{
    private const string StorageFolder = "medical-cvs";
    private readonly string _storageRoot = Path.GetFullPath(
        Path.Combine(hostEnvironment.ContentRootPath, StorageFolder));

    public async Task<string> SaveAsync(
        byte[] pdfBytes,
        Guid medicalCvId,
        int versionNumber,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pdfBytes);

        if (pdfBytes.Length == 0)
        {
            throw new ArgumentException(
                "PDF content cannot be empty.",
                nameof(pdfBytes));
        }

        if (medicalCvId == Guid.Empty)
        {
            throw new ArgumentException(
                "A medical CV ID is required.",
                nameof(medicalCvId));
        }

        if (versionNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(versionNumber));
        }

        var cvDirectory = Path.Combine(_storageRoot, medicalCvId.ToString());
        Directory.CreateDirectory(cvDirectory);

        var fileName = $"version-{versionNumber}.pdf";
        var absolutePath = Path.Combine(cvDirectory, fileName);

        try
        {
            await using var destination = new FileStream(
                absolutePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);
            await destination.WriteAsync(pdfBytes, cancellationToken);
        }
        catch
        {
            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }

            throw;
        }

        return $"{StorageFolder}/{medicalCvId}/{fileName}";
    }

    public Task<bool> DeleteAsync(
        string fileKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var absolutePath = Path.GetFullPath(
            Path.Combine(hostEnvironment.ContentRootPath, fileKey));
        var expectedPrefix = _storageRoot + Path.DirectorySeparatorChar;

        if (!absolutePath.StartsWith(
                expectedPrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The medical CV file path is outside the configured storage folder.");
        }

        if (!File.Exists(absolutePath))
        {
            return Task.FromResult(false);
        }

        File.Delete(absolutePath);
        return Task.FromResult(true);
    }
}
