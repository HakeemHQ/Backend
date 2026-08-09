using Hakeem.Domain.Enums.MedicalCvs;

namespace Hakeem.Application.Features.MedicalCvs.Queries.GetMedicalCvById;

public sealed record GetMedicalCvByIdResponse(
    Guid MedicalCvId,
    string Title,
    MedicalCvScopeType ScopeType,
    string? Focus,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<MedicalCvVersionDetailResponse> Versions);

public sealed record MedicalCvVersionDetailResponse(
    Guid MedicalCvVersionId,
    int VersionNumber,
    MedicalCvVersionStatus Status,
    DateTime CreatedAt,
    DateTime? ApprovedAt,
    bool PdfAvailable);
