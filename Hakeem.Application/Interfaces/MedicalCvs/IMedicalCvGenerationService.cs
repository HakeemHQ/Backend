using Hakeem.Application.Features.MedicalCvs.DTOs;
using Hakeem.Domain.Enums.MedicalCvs;

namespace Hakeem.Application.Interfaces.MedicalCvs;

public interface IMedicalCvGenerationService
{
    Task<MedicalCvGenerationResult> GenerateFullAsync(
        Guid patientId,
        string title,
        string language,
        MedicalCvCreatedByRole createdByRole,
        CancellationToken cancellationToken = default);

    Task<MedicalCvGenerationResult> GenerateFocusedAsync(
        Guid patientId,
        string focus,
        string title,
        IReadOnlyList<MedicalCvEvidenceItem> evidence,
        string language,
        MedicalCvCreatedByRole createdByRole,
        CancellationToken cancellationToken = default);
}
