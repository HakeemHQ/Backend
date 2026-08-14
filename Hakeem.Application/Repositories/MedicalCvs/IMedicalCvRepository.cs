using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.MedicalCvs;
using System;
using static Hakeem.Application.Features.MedicalCvs.Queries.GetMedicalCvPdf.GetMedicalCvPdfQueryHandler;

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

    Task<MedicalCvVersion?> GetVersionForPatientAsync(
        Guid medicalCvVersionId,
        Guid patientId,
        CancellationToken cancellationToken);

    Task<MedicalCvVersion?> GetVersionForGenerationAsync(
        Guid medicalCvVersionId,
        CancellationToken cancellationToken);

    void Add(MedicalCv medicalCv);
    void AddVersion(MedicalCvVersion version);

    Task<IReadOnlyList<MedicalCv>> GetByPatientIdAsync(
    Guid patientId,
    CancellationToken cancellationToken);

    Task<MedicalCvVersionReadModel?> GetVersionByIdAsync(
    Guid medicalCvVersionId,
    CancellationToken cancellationToken);

    Task<MedicalCvReadModel?> GetByIdAsync(
        Guid medicalCvId,
        CancellationToken cancellationToken);

    
Task<MedicalCvVersion?> GetVersionForApprovalAsync(
    Guid medicalCvVersionId,
    CancellationToken cancellationToken);

    public sealed record MedicalCvVersionReadModel(
        Guid MedicalCvVersionId,
        Guid MedicalCvId,
        MedicalCvVersionStatus Status,
        string? PdfFileKey);
}
