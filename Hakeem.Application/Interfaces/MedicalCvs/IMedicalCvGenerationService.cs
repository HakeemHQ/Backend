using Hakeem.Application.Features.MedicalCvs.DTOs;

namespace Hakeem.Application.Interfaces.MedicalCvs;

public interface IMedicalCvGenerationService
{
    Task<MedicalCvGenerationResult> GenerateFullAsync(
        Guid patientId,
        string title,
        string language,
        CancellationToken cancellationToken = default);

    Task<MedicalCvGenerationResult> GenerateFocusedAsync(
        Guid patientId,
        string focus,
        string title,
        IReadOnlyList<MedicalCvEvidenceItem> evidence,
        string language,
        CancellationToken cancellationToken = default);
}
