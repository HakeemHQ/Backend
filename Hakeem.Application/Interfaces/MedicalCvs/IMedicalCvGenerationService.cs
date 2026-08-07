using Hakeem.Application.Features.MedicalCvs.DTOs;

namespace Hakeem.Application.Interfaces.MedicalCvs;

public interface IMedicalCvGenerationService
{
    Task<MedicalCvGenerationResult> GenerateFullAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<MedicalCvGenerationResult> GenerateFocusedAsync(
        Guid patientId,
        string focus,
        CancellationToken cancellationToken = default);
}
