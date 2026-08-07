using Hakeem.Application.Features.MedicalCvs.DTOs;
using Hakeem.Application.Interfaces.MedicalCvs;
using Hakeem.Domain.Interfaces.ServiceLifetime;
using Microsoft.Extensions.Logging;

namespace Hakeem.Infrastructure.Rag;

public sealed class DummyFocusedMedicalEvidenceProvider(
    ILogger<DummyFocusedMedicalEvidenceProvider> logger)
    : IFocusedMedicalEvidenceProvider, IScoped
{
    private static readonly Guid DummyPointId =
        Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");

    public Task<FocusedMedicalEvidenceResponse> SearchAsync(
        Guid patientId,
        string focus,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogWarning(
            "Using dummy focused medical evidence for patient {PatientId} and focus {Focus}. Replace this provider when RAG search is available.",
            patientId,
            focus);

        FocusedMedicalEvidenceResponse response = new(
            GlobalErrorCode: string.Empty,
            Data:
            [
                new FocusedMedicalEvidence(
                    DummyPointId,
                    Score: 1,
                    Content: $"Dummy confirmed medical record relevant to the requested focus: {focus}",
                    FieldName: "Focus",
                    Value: focus)
            ]);

        return Task.FromResult(response);
    }
}
