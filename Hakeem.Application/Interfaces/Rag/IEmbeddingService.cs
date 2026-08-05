using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Interfaces.Rag;

public interface IEmbeddingService : IScoped
{
    Task<float[]> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken);
}
