using Hakeem.Domain.Interfaces.ServiceLifetime;
using Microsoft.AspNetCore.Http;

namespace Hakeem.Application.Interfaces.Files;

public interface IDocumentFileStorage : IScoped
{
    Task<string> SaveAsync(
        IFormFile file,
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}
