using Hakeem.Domain.Enums.MedicalCvs;

namespace Hakeem.Application.Features.MedicalCvs.Queries.GetMedicalCvs;

public sealed record GetMedicalCvsResponse(
    IReadOnlyList<MedicalCvListItemResponse> Items,
    PaginationResponse Pagination);

public sealed record MedicalCvListItemResponse(
    Guid MedicalCvId,
    string Title,
    MedicalCvScopeType ScopeType,
    string? Focus,
    LatestMedicalCvVersionResponse? LatestVersion);

public sealed record LatestMedicalCvVersionResponse(
    Guid MedicalCvVersionId,
    int VersionNumber,
    MedicalCvVersionStatus Status);

public sealed record PaginationResponse(
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);
