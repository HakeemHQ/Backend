using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.MedicalCvs;

namespace Hakeem.Application.Repositories.MedicalCvs;

public interface IMedicalCvRepository
{
    Task<MedicalCv?> GetByLogicalIdentityAsync(
        Guid patientId,
        MedicalCvScopeType scopeType,
        string? focus,
        CancellationToken cancellationToken);

    Task<int> GetNextVersionNumberAsync(
        Guid medicalCvId,
        CancellationToken cancellationToken);

    void Add(MedicalCv medicalCv);
    void AddVersion(MedicalCvVersion version);
}
