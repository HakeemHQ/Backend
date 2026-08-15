using Hakeem.Application.Configurations;
using Hakeem.Application.Common;
using Hakeem.Application.Features.MedicalCvs.DTOs;
using Hakeem.Application.Interfaces.MedicalCvs;
using Hakeem.Application.Interfaces.Rag;
using Hakeem.Application.Repositories.MedicalRecords;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.MedicalCvs;
using Hakeem.Infrastructure.AI.Agents.Plugins;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Hakeem.Infrastructure.Tests.AI.Agents;

public sealed class MedicalIntelligencePluginTests
{
    [Fact]
    public async Task SearchPatientMedicalRecordsAsync_UsesPatientAndConfiguredLimit()
    {
        var patientId = Guid.NewGuid();
        var searchService = new FakeMedicalRecordSearchService(
        [
            CreateSearchResult(patientId, 0.8f, "Confirmed"),
            CreateSearchResult(Guid.NewGuid(), 0.9f, "Confirmed")
        ]);
        var plugin = CreatePlugin(
            patientId,
            searchService,
            new FakeMedicalCvGenerationService());

        var result = await plugin.SearchPatientMedicalRecordsAsync(
            "  current diabetes medication  ",
            CancellationToken.None);

        Assert.Equal("current diabetes medication", searchService.Query);
        Assert.Equal(patientId, searchService.PatientId);
        Assert.Equal(15, searchService.Limit);
        Assert.Single(result.Records);
        Assert.Equal("Medication", result.Records[0].RecordType);
    }

    [Fact]
    public async Task GenerateFocusedMedicalCvAsync_FiltersAndMapsEvidence()
    {
        var patientId = Guid.NewGuid();
        var diagnosis = CreateSearchResult(
            patientId,
            0.82f,
            "Confirmed",
            "Diagnosis",
            "Type 2 Diabetes Mellitus");
        var medication = CreateSearchResult(
            patientId,
            0.42f,
            "Confirmed",
            "Medication",
            "MedicationName: Metformin, Dose: 500 mg");
        var allergy = CreateAllergy(patientId);
        var searchService = new FakeMedicalRecordSearchService(
            directResults:
            [
                diagnosis,
                CreateSearchResult(
                    patientId,
                    0.95f,
                    "Pending",
                    "Diagnosis",
                    "Pending diagnosis"),
                CreateSearchResult(
                    Guid.NewGuid(),
                    0.99f,
                    "Confirmed",
                    "Diagnosis",
                    "Another patient's diagnosis")
            ],
            medicationResults: [medication]);
        var generationService = new FakeMedicalCvGenerationService();
        var plugin = CreatePlugin(
            patientId,
            searchService,
            generationService,
            [allergy]);

        var result = await plugin.GenerateFocusedMedicalCvAsync(
            "  Diabetes  ",
            "  Diabetes Medical CV  ",
            "en",
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, searchService.Calls.Count);
        Assert.Collection(
            searchService.Calls,
            call =>
            {
                Assert.Equal("Diabetes", call.Query);
            },
            call =>
            {
                Assert.Equal(
                    "medications and treatments used for Diabetes",
                    call.Query);
            });
        Assert.Equal(patientId, generationService.PatientId);
        Assert.Equal("Diabetes", generationService.Focus);
        Assert.Equal("Diabetes Medical CV", generationService.Title);
        Assert.Equal("en", generationService.Language);
        Assert.Equal(3, generationService.Evidence!.Count);
        Assert.Contains(
            generationService.Evidence,
            item => item.PointId == diagnosis.MedicalRecordId &&
                    item.FieldName == "Diagnosis" &&
                    item.Value == "2026-08-06");
        Assert.Contains(
            generationService.Evidence,
            item => item.PointId == medication.MedicalRecordId &&
                    item.FieldName == "Medication" &&
                    item.Score == 0.42f);
        Assert.Contains(
            generationService.Evidence,
            item => item.PointId == allergy.Id &&
                    item.FieldName == "Allergy" &&
                    item.Score is null &&
                    item.Content.Contains("Penicillin"));
        Assert.NotNull(result.MedicalCv);
        Assert.Equal(3, result.MedicalCv.VersionNumber);
        Assert.Equal(MedicalCvVersionStatus.Draft, result.MedicalCv.Status);
    }

    [Fact]
    public async Task GenerateFocusedMedicalCvAsync_WithoutEligibleEvidence_DoesNotGenerate()
    {
        var patientId = Guid.NewGuid();
        var generationService = new FakeMedicalCvGenerationService();
        var plugin = CreatePlugin(
            patientId,
            new FakeMedicalRecordSearchService(
                directResults:
                [
                    CreateSearchResult(
                        patientId,
                        0.49f,
                        "Confirmed",
                        "Diagnosis",
                        "Type 2 Diabetes Mellitus"),
                    CreateSearchResult(
                        patientId,
                        0.99f,
                        "Pending",
                        "Diagnosis",
                        "Pending diagnosis")
                ],
                medicationResults:
                [
                    CreateSearchResult(
                        patientId,
                        0.34f,
                        "Confirmed",
                        "Medication",
                        "MedicationName: Metformin")
                ]),
            generationService,
            [CreateAllergy(patientId)]);

        var result = await plugin.GenerateFocusedMedicalCvAsync(
            "Diabetes",
            "Diabetes Medical CV",
            "ar",
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Null(result.MedicalCv);
        Assert.Equal(
            "لم يتم العثور على سجلات طبية مؤكدة ومرتبطة بهذا الموضوع بدرجة كافية.",
            result.Message);
        Assert.Equal(0, generationService.CallCount);
    }

    private static MedicalIntelligencePlugin CreatePlugin(
        Guid patientId,
        IMedicalRecordSearchService searchService,
        IMedicalCvGenerationService generationService,
        IReadOnlyList<MedicalRecord>? allergies = null) =>
        new(
            patientId,
            searchService,
            new FakeMedicalRecordsRepository(allergies ?? []),
            generationService,
            Options.Create(
                new MedicalIntelligenceAgentConfiguration
                {
                    SearchLimit = 15,
                    FocusedCvMinimumScore = 0.5f,
                    FocusedCvRelatedEvidenceMinimumScore = 0.35f
                }),
            NullLogger<MedicalIntelligencePlugin>.Instance);

    private static MedicalRecordSearchResult CreateSearchResult(
        Guid patientId,
        float score,
        string status,
        string recordType = "Medication",
        string displayName = "Dose: 10 mg, MedicationName: Lisinopril") =>
        new(
            Guid.NewGuid(),
            patientId,
            score,
            recordType,
            displayName,
            status,
            new DateTime(2026, 8, 6, 17, 3, 55, DateTimeKind.Utc),
            $"{displayName} | Dose: 10 mg; MedicationName: Lisinopril",
            "Dose: 10 mg; MedicationName: Lisinopril");

    private static MedicalRecord CreateAllergy(Guid patientId)
    {
        var allergy = new MedicalRecord
        {
            Id = Guid.NewGuid(),
            PatientProfileId = patientId,
            RecordType = "Allergy",
            DisplayName = "AllergyName: Penicillin",
            Status = "Confirmed",
            ClinicalDate = new DateTime(
                2026,
                8,
                6,
                17,
                3,
                55,
                DateTimeKind.Utc)
        };
        allergy.Fields.Add(new MedicalRecordField
        {
            Id = Guid.NewGuid(),
            FieldName = "AllergyName",
            Value = "Penicillin"
        });
        return allergy;
    }

    private sealed class FakeMedicalRecordSearchService(
        IReadOnlyList<MedicalRecordSearchResult> directResults,
        IReadOnlyList<MedicalRecordSearchResult>? medicationResults = null)
        : IMedicalRecordSearchService
    {
        private readonly IReadOnlyList<MedicalRecordSearchResult>
            _medicationResults = medicationResults ?? directResults;

        public string? Query { get; private set; }
        public Guid PatientId { get; private set; }
        public int Limit { get; private set; }
        public List<SearchCall> Calls { get; } = [];

        public Task<IReadOnlyList<MedicalRecordSearchResult>> SearchAsync(
            string query,
            Guid patientProfileId,
            int limit,
            CancellationToken cancellationToken)
        {
            Query = query;
            PatientId = patientProfileId;
            Limit = limit;
            Calls.Add(new SearchCall(query));
            var results = query.StartsWith(
                "medications and treatments used for ",
                StringComparison.OrdinalIgnoreCase)
                ? _medicationResults
                : directResults;
            return Task.FromResult(results);
        }
    }

    private sealed record SearchCall(string Query);

    private sealed class FakeMedicalRecordsRepository(
        IReadOnlyList<MedicalRecord> allergies)
        : IMedicalRecordsRepository
    {
        public Task<IReadOnlyList<MedicalRecord>>
            GetConfirmedByRecordTypeAsync(
                Guid patientProfileId,
                string recordType,
                CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MedicalRecord>>(
                string.Equals(
                    recordType,
                    "Allergy",
                    StringComparison.OrdinalIgnoreCase)
                    ? allergies
                    : []);

        public Task<IReadOnlyList<MedicalRecord>> GetAllConfirmedAsync(
            Guid patientProfileId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PaginatedResult<MedicalRecord>> GetMedicalRecordsAsync(
            Guid patientProfileId,
            string? search,
            string? recordType,
            DateTime? fromDate,
            DateTime? toDate,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public void Add(MedicalRecord record) =>
            throw new NotSupportedException();

        public Task<MedicalRecord?> GetBySourceExtractedItemIdAsync(
            Guid extractedItemId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<MedicalRecord?> GetMedicalRecordByIdAsync(
            Guid medicalRecordId,
            Guid patientProfileId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<MedicalRecord?> GetByIdWithFieldsAsync(
            Guid medicalRecordId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<MedicalRecord?> GetByIdWithDetailsAsync(
            Guid medicalRecordId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeMedicalCvGenerationService
        : IMedicalCvGenerationService
    {
        public int CallCount { get; private set; }
        public Guid PatientId { get; private set; }
        public string? Focus { get; private set; }
        public string? Title { get; private set; }
        public string? Language { get; private set; }
        public IReadOnlyList<MedicalCvEvidenceItem>? Evidence { get; private set; }

        public Task<MedicalCvGenerationResult> GenerateFullAsync(
            Guid patientId,
            string title,
            string language,
            MedicalCvCreatedByRole createdByRole,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<MedicalCvGenerationResult> GenerateFocusedAsync(
            Guid patientId,
            string focus,
            string title,
            IReadOnlyList<MedicalCvEvidenceItem> evidence,
            string language,
            MedicalCvCreatedByRole createdByRole,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            PatientId = patientId;
            Focus = focus;
            Title = title;
            Language = language;
            Evidence = evidence;

            return Task.FromResult(new MedicalCvGenerationResult(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                title,
                3,
                MedicalCvScopeType.Focused,
                focus,
                "internal/file.pdf",
                MedicalCvVersionStatus.Draft,
                new DateTime(2026, 8, 8, 10, 0, 0, DateTimeKind.Utc)));
        }
    }
}
