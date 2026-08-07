using Hakeem.Application.Features.MedicalCvs.DTOs;

namespace Hakeem.Application.Interfaces.MedicalCvs;

public interface IMedicalCvContentGenerator
{
    Task<MedicalCvContent> GenerateAsync(
        MedicalCvContentRequest request,
        CancellationToken cancellationToken = default);
}
