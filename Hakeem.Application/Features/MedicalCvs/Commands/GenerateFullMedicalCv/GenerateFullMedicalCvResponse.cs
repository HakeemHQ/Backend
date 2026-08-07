using Hakeem.Domain.Enums.MedicalCvs;

namespace Hakeem.Application.Features.MedicalCvs.Commands.GenerateFullMedicalCv;

public sealed record GenerateFullMedicalCvResponse(
    Guid MedicalCvId,
    string Title,
    MedicalCvScopeType ScopeType,
    LatestMedicalCvVersionResponse LatestVersion);

public sealed record LatestMedicalCvVersionResponse(
    Guid MedicalCvVersionId,
    int VersionNumber,
    string Status,
    DateTime CreatedAt,
    string PdfUrl,
    DateTimeOffset PreviewExpiresAt);
