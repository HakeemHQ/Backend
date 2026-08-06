using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Interfaces.Rag;

public interface IMedicalRecordVectorStore : IScoped
{
    Task UpsertAsync(
        MedicalRecordVectorDocument document,
        float[] embedding,
        CancellationToken cancellationToken);
}
