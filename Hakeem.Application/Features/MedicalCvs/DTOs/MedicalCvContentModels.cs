using Hakeem.Domain.Enums.MedicalCvs;

namespace Hakeem.Application.Features.MedicalCvs.DTOs;

public sealed record MedicalCvPatientInformation(
    string FullName,
    DateTime BirthDate,
    string Gender,
    string Email,
    string PhoneNumber);

public sealed record MedicalCvEvidenceItem(
    Guid PointId,
    double? Score,
    string Content,
    string? FieldName,
    string? Value);

public sealed record MedicalCvContentRequest(
    MedicalCvPatientInformation Patient,
    MedicalCvScopeType ScopeType,
    string? Focus,
    IReadOnlyList<MedicalCvEvidenceItem> Evidence,
    string? Title = null,
    string Language = MedicalCvLanguages.English);

public sealed class MedicalCvContent
{
    public string Title { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public List<MedicalCvSection> Sections { get; init; } = [];
}

public sealed class MedicalCvSection
{
    public string Heading { get; init; } = string.Empty;
    public List<MedicalCvEntry> Entries { get; init; } = [];
}

public sealed class MedicalCvEntry
{
    public string Title { get; init; } = string.Empty;
    public string? Date { get; init; }
    public List<string> Details { get; init; } = [];
}

public sealed record MedicalCvPdfDocument(
    MedicalCvPatientInformation Patient,
    MedicalCvScopeType ScopeType,
    string? Focus,
    DateTime GeneratedAtUtc,
    MedicalCvContent Content,
    string Language = MedicalCvLanguages.English);

public static class MedicalCvLanguages
{
    public const string English = "en";
    public const string Arabic = "ar";

    public static string Normalize(string? language) =>
        string.Equals(language?.Trim(), Arabic, StringComparison.OrdinalIgnoreCase)
            ? Arabic
            : English;
}

public sealed record MedicalCvGenerationResult(
    Guid MedicalCvId,
    Guid MedicalCvVersionId,
    string Title,
    int VersionNumber,
    MedicalCvScopeType ScopeType,
    string? Focus,
    string PdfFileKey,
    MedicalCvVersionStatus Status,
    DateTime CreatedAt);
