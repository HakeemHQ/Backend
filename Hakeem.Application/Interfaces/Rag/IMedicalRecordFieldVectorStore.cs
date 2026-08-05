using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Interfaces.Rag;

public interface IMedicalRecordFieldVectorStore : IScoped
{
    Task UpsertAsync(
        MedicalRecordFieldVectorDocument document,
        float[] embedding,
        CancellationToken cancellationToken);
}
