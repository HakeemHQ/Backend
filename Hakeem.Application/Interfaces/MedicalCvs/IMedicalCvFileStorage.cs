namespace Hakeem.Application.Interfaces.MedicalCvs;

public interface IMedicalCvFileStorage
{
    Task<string> SaveAsync(
        byte[] pdfBytes,
        Guid medicalCvId,
        int versionNumber,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string fileKey,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(
        string fileKey,
        CancellationToken cancellationToken = default);
}
