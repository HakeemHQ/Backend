using Hakeem.Application.Features.MedicalCvs.DTOs;

namespace Hakeem.Application.Interfaces.MedicalCvs;

public interface IFocusedMedicalEvidenceProvider
{
    Task<FocusedMedicalEvidenceResponse> SearchAsync(
        Guid patientId,
        string focus,
        CancellationToken cancellationToken = default);
}
