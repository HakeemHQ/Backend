namespace Hakeem.Application.Features.MedicalCvs.Commands.GenerateFullMedicalCv;

public sealed record GenerateFullMedicalCvResponse(
    Guid MedicalCvId,
    string Title,
    LatestMedicalCvVersionResponse LatestVersion);

public sealed record LatestMedicalCvVersionResponse(
    Guid MedicalCvVersionId,
    int VersionNumber,
    string GenerationStatus,
    string VerificationStatus,
    string CreatedByRole);
