using System.ComponentModel;
using System.Globalization;
using Hakeem.Application.Configurations;
using Hakeem.Application.Features.MedicalCvs.DTOs;
using Hakeem.Application.Interfaces.Agents;
using Hakeem.Application.Interfaces.MedicalCvs;
using Hakeem.Application.Interfaces.Rag;
using Hakeem.Application.Repositories.MedicalRecords;
using Hakeem.Application.Resources;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.MedicalCvs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace Hakeem.Infrastructure.AI.Agents.Plugins;

public sealed class MedicalIntelligencePlugin(
    Guid patientId,
    IMedicalRecordSearchService medicalRecordSearchService,
    IMedicalRecordsRepository medicalRecordsRepository,
    IMedicalCvGenerationService medicalCvGenerationService,
    IOptions<MedicalIntelligenceAgentConfiguration> options,
    ILogger<MedicalIntelligencePlugin> logger)
{
    private readonly MedicalIntelligenceAgentConfiguration _configuration =
        options.Value;

    public PatientMedicalEvidenceToolResult? LastPatientEvidenceResult
        { get; private set; }

    public string? LastPatientEvidenceQuery { get; private set; }

    public FocusedMedicalCvToolResult? LastFocusedMedicalCvResult
        { get; private set; }

    public string? LastFocusedMedicalCvFocus { get; private set; }

    public string? LastFocusedMedicalCvTitle { get; private set; }

    public string? LastFocusedMedicalCvLanguage { get; private set; }

    [KernelFunction("search_patient_medical_records")]
    [Description(
        "Searches the current patient's medical records for evidence relevant " +
        "to a factual patient-record question. Use only for questions about " +
        "the patient's own stored medical information.")]
    public async Task<PatientMedicalEvidenceToolResult>
        SearchPatientMedicalRecordsAsync(
            [Description(
                "A concise, explicit semantic-search query preserving the facts " +
                "and time qualifiers requested by the patient.")]
            string query,
            CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        LastPatientEvidenceQuery = query.Trim();
        var results = await medicalRecordSearchService.SearchAsync(
            LastPatientEvidenceQuery,
            patientId,
            _configuration.SearchLimit,
            cancellationToken);

        LastPatientEvidenceResult = new PatientMedicalEvidenceToolResult(
            results
                .Where(result => result.PatientProfileId == patientId)
                .Select(result => new PatientMedicalEvidenceItem(
                    result.MedicalRecordId,
                    result.Score,
                    result.RecordType,
                    result.DisplayName,
                    result.Status,
                    result.ClinicalDate,
                    result.Content,
                    result.Fields))
                .ToArray());

        logger.LogInformation(
            "Retrieved {EvidenceCount} medical-record results for patient {PatientId}.",
            LastPatientEvidenceResult.Records.Count,
            patientId);

        return LastPatientEvidenceResult;
    }

    [KernelFunction("generate_focused_medical_cv")]
    [Description(
        "Creates a focused medical CV for the current patient from relevant, " +
        "confirmed medical-record evidence. Use only for an explicit request " +
        "to create or generate a focused medical CV.")]
    public async Task<FocusedMedicalCvToolResult> GenerateFocusedMedicalCvAsync(
        [Description(
            "The concise medical topic or condition the focused CV should cover.")]
        string focus,
        [Description(
            "The patient-facing CV title, usually '<Focus> Medical CV'.")]
        string title,
        [Description(
            "The requested patient-facing output language as ISO code 'ar' or 'en'.")]
        string language,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(focus);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        var normalizedFocus = focus.Trim();
        var normalizedTitle = title.Trim();
        var normalizedLanguage = MedicalCvLanguages.Normalize(language);
        LastFocusedMedicalCvFocus = normalizedFocus;
        LastFocusedMedicalCvTitle = normalizedTitle;
        LastFocusedMedicalCvLanguage = normalizedLanguage;
        var evidence = await RetrieveFocusedEvidenceAsync(
            normalizedFocus,
            cancellationToken);

        if (evidence.Count == 0)
        {
            LastFocusedMedicalCvResult = new FocusedMedicalCvToolResult(
                false,
                LocalizedResourceText.Get(
                    "MedicalCv.Focused.NoEvidence",
                    normalizedLanguage),
                null);

            logger.LogInformation(
                "Focused medical CV was not generated for patient {PatientId}; no evidence met score {MinimumScore}.",
                patientId,
                _configuration.FocusedCvMinimumScore);

            return LastFocusedMedicalCvResult;
        }

        var result = await medicalCvGenerationService.GenerateFocusedAsync(
            patientId,
            normalizedFocus,
            normalizedTitle,
            evidence,
            normalizedLanguage,
            MedicalCvCreatedByRole.Patient,
            cancellationToken);

        var actionResult = new FocusedMedicalCvActionResult(
            result.MedicalCvId,
            result.MedicalCvVersionId,
            result.Title,
            result.Focus ?? normalizedFocus,
            result.VersionNumber,
            result.Status,
            result.CreatedAt);

        LastFocusedMedicalCvResult = new FocusedMedicalCvToolResult(
            true,
            LocalizedResourceText.Get(
                "MedicalCv.Focused.Created",
                normalizedLanguage),
            actionResult);

        return LastFocusedMedicalCvResult;
    }

    private async Task<IReadOnlyList<MedicalCvEvidenceItem>>
        RetrieveFocusedEvidenceAsync(
            string focus,
            CancellationToken cancellationToken)
    {
        var directEvidence = await medicalRecordSearchService.SearchAsync(
            focus,
            patientId,
            _configuration.SearchLimit,
            cancellationToken);
        var medicationEvidence = await medicalRecordSearchService.SearchAsync(
            $"medications and treatments used for {focus}",
            patientId,
            _configuration.SearchLimit,
            cancellationToken);
        var allergyEvidence = await medicalRecordsRepository
            .GetConfirmedByRecordTypeAsync(
                patientId,
                "Allergy",
                cancellationToken);

        var evidenceByRecordId = new Dictionary<
            Guid,
            MedicalCvEvidenceItem>();

        AddSearchEvidence(
            evidenceByRecordId,
            directEvidence,
            _configuration.FocusedCvMinimumScore,
            requiredRecordType: null);
        AddSearchEvidence(
            evidenceByRecordId,
            medicationEvidence,
            _configuration.FocusedCvRelatedEvidenceMinimumScore,
            requiredRecordType: "Medication");

        if (evidenceByRecordId.Count == 0)
        {
            // Allergies supplement a focused CV but must not create one when
            // no evidence is related to the requested focus.
            return [];
        }

        foreach (var allergy in allergyEvidence)
        {
            // Canonical confirmed allergies are included regardless of vector
            // similarity because their safety relevance is not focus-specific.
            evidenceByRecordId[allergy.Id] = MapStructuredRecord(allergy);
        }

        logger.LogInformation(
            "Assembled {EvidenceCount} focused-CV records for patient {PatientId}, including {MedicationCount} medications and {AllergyCount} allergies.",
            evidenceByRecordId.Count,
            evidenceByRecordId.Values.Count(item =>
                string.Equals(
                    item.FieldName,
                    "Medication",
                    StringComparison.OrdinalIgnoreCase)),
            evidenceByRecordId.Values.Count(item =>
                string.Equals(
                    item.FieldName,
                    "Allergy",
                    StringComparison.OrdinalIgnoreCase)),
            patientId);

        return evidenceByRecordId.Values.ToArray();
    }

    private void AddSearchEvidence(
        IDictionary<Guid, MedicalCvEvidenceItem> evidenceByRecordId,
        IReadOnlyList<MedicalRecordSearchResult> results,
        float minimumScore,
        string? requiredRecordType)
    {
        foreach (var result in results.Where(result =>
                     result.PatientProfileId == patientId &&
                     result.Score >= minimumScore &&
                     string.Equals(
                         result.Status,
                         "Confirmed",
                         StringComparison.OrdinalIgnoreCase) &&
                     (requiredRecordType is null ||
                      string.Equals(
                          result.RecordType,
                          requiredRecordType,
                          StringComparison.OrdinalIgnoreCase))))
        {
            var candidate = MapEvidence(result);

            if (string.IsNullOrWhiteSpace(candidate.Content))
            {
                continue;
            }

            if (!evidenceByRecordId.TryGetValue(
                    result.MedicalRecordId,
                    out var existing) ||
                candidate.Score.GetValueOrDefault() >
                existing.Score.GetValueOrDefault())
            {
                evidenceByRecordId[result.MedicalRecordId] = candidate;
            }
        }
    }

    private static MedicalCvEvidenceItem MapEvidence(
        MedicalRecordSearchResult result)
    {
        var content = FirstNotBlank(
            result.Content,
            result.DisplayName,
            result.Fields) ?? string.Empty;
        var clinicalDate = result.ClinicalDate?.ToString(
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture);

        return new MedicalCvEvidenceItem(
            result.MedicalRecordId,
            result.Score,
            content,
            result.RecordType,
            clinicalDate);
    }

    private static MedicalCvEvidenceItem MapStructuredRecord(
        MedicalRecord record)
    {
        var fields = string.Join(
            "; ",
            record.Fields.Select(field =>
                $"{field.FieldName}: {field.Value}"));
        var content = string.IsNullOrWhiteSpace(fields)
            ? record.DisplayName
            : $"{record.DisplayName} | {fields}";

        return new MedicalCvEvidenceItem(
            record.Id,
            Score: null,
            content,
            record.RecordType,
            record.ClinicalDate.ToString(
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture));
    }

    private static string? FirstNotBlank(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}

public sealed record PatientMedicalEvidenceToolResult(
    IReadOnlyList<PatientMedicalEvidenceItem> Records);

public sealed record FocusedMedicalCvToolResult(
    bool Success,
    string Message,
    FocusedMedicalCvActionResult? MedicalCv);
