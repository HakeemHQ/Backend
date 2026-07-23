using Hakeem.Domain.Interfaces.ServiceLifetime;

using Microsoft.AspNetCore.Http;

namespace Hakeem.Application.Interfaces.Files;

public interface IFileService : IScoped
{
    Task<string> SaveFileAsync(IFormFile file, string[] Path, CancellationToken cancellationToken = default);

    Task<string> SaveFileAsync(
        IFormFile file,
        string module,
        string entityType,
        int entityId,
        string fileCategory,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteFileAsync(
        string? filePath,
        CancellationToken cancellationToken = default);

    Task<string> UpdateFileAsync(
        IFormFile newFile,
        string module,
        string? oldFilePath,
        string entityType,
        int entityId,
        string fileCategory,
        CancellationToken cancellationToken = default);
}



