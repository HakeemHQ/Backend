using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Interfaces.Rag;

public interface IMedicalRecordSearchService : IScoped
{
    Task<IReadOnlyList<MedicalRecordSearchResult>> SearchAsync(
        string query,
        Guid patientProfileId,
        int limit,
        CancellationToken cancellationToken);
}
