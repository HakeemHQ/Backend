using Hakeem.Application.Common;
using Hakeem.Domain.Enums.MedicalCvs;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Repositories.MedicalCvs;

public interface IMedicalCvReadRepository : IScoped
{
    Task<PaginatedResult<MedicalCvListReadModel>> GetForPatientAsync(
        Guid patientId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<MedicalCvDetailReadModel?> GetDetailForPatientAsync(
        Guid medicalCvId,
        Guid patientId,
        CancellationToken cancellationToken);

    Task<MedicalCvReadModel?> GetByIdAsync(
    Guid medicalCvId,
    CancellationToken cancellationToken);


}
public sealed record MedicalCvReadModel(
    Guid MedicalCvId,
    Guid PatientId);
public sealed record MedicalCvListReadModel(
    Guid MedicalCvId,
    string Title,
    MedicalCvScopeType ScopeType,
    string? Focus,
    LatestMedicalCvVersionReadModel? LatestVersion);

public sealed record LatestMedicalCvVersionReadModel(
    Guid MedicalCvVersionId,
    int VersionNumber,
    MedicalCvVersionStatus Status);

public sealed record MedicalCvDetailReadModel(
    Guid MedicalCvId,
    string Title,
    MedicalCvScopeType ScopeType,
    string? Focus,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<MedicalCvVersionDetailReadModel> Versions);

public sealed record MedicalCvVersionDetailReadModel(
    Guid MedicalCvVersionId,
    int VersionNumber,
    MedicalCvVersionStatus Status,
    DateTime CreatedAt,
    DateTime? ApprovedAt,
    bool PdfAvailable);

