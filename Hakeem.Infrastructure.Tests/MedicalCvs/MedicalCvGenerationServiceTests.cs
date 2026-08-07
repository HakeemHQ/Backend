using Hakeem.Application.Common;
using Hakeem.Application.Features.MedicalCvs.DTOs;
using Hakeem.Application.Interfaces.MedicalCvs;
using Hakeem.Application.Repositories.MedicalCvs;
using Hakeem.Application.Repositories.MedicalRecords;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Application.Services.MedicalCvs;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.MedicalCvs;
using Hakeem.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hakeem.Infrastructure.Tests.MedicalCvs;

public sealed class MedicalCvGenerationServiceTests
{
    [Fact]
    public async Task GenerateFullAsync_UsesConfirmedRecordDisplayNamesAndSavesVersion()
    {
        var patient = CreatePatient();
        var confirmedRecord = new MedicalRecord
        {
            Id = Guid.NewGuid(),
            PatientProfileId = patient.Id,
            RecordType = "LabResult",
            DisplayName = "HbA1c, 7.2%, 2026-07-15",
            ClinicalDate = new DateTime(2026, 7, 15),
            Status = "Confirmed"
        };
        confirmedRecord.Fields.Add(new MedicalRecordField
        {
            Id = Guid.NewGuid(),
            FieldName = "This field must not be read",
            Value = "Not included"
        });

        var recordsRepository = new FakeMedicalRecordsRepository([confirmedRecord]);
        var cvRepository = new FakeMedicalCvRepository();
        var contentGenerator = new FakeContentGenerator();
        var evidenceProvider = new FakeFocusedEvidenceProvider();
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(
            patient,
            recordsRepository,
            cvRepository,
            evidenceProvider,
            contentGenerator,
            unitOfWork);

        var result = await service.GenerateFullAsync(patient.Id);

        Assert.Equal(1, recordsRepository.GetAllConfirmedCallCount);
        Assert.Equal(0, evidenceProvider.CallCount);
        Assert.NotNull(contentGenerator.Request);
        Assert.Equal(MedicalCvScopeType.Full, contentGenerator.Request.ScopeType);
        var evidence = Assert.Single(contentGenerator.Request.Evidence);
        Assert.Equal(confirmedRecord.DisplayName, evidence.Content);
        Assert.DoesNotContain("This field must not be read", evidence.Content);
        Assert.NotNull(cvRepository.AddedMedicalCv);
        Assert.Equal(MedicalCvScopeType.Full, cvRepository.AddedMedicalCv.ScopeType);
        Assert.Null(cvRepository.AddedMedicalCv.Focus);
        Assert.NotNull(cvRepository.AddedVersion);
        Assert.Same(
            confirmedRecord,
            Assert.Single(cvRepository.AddedVersion.SummarizedRecords));
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Equal(
            $"medical-cvs/{result.MedicalCvId}/version-1.pdf",
            result.PdfFileKey);
    }

    [Fact]
    public async Task GenerateFocusedAsync_UsesOnlyFocusedEvidenceAndKeepsFocus()
    {
        var patient = CreatePatient();
        var existingCv = new MedicalCv
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            Title = "Existing CV",
            ScopeType = MedicalCvScopeType.Focused,
            Focus = "Diabetes"
        };
        var recordsRepository = new FakeMedicalRecordsRepository([]);
        var cvRepository = new FakeMedicalCvRepository(existingCv, nextVersion: 4);
        var contentGenerator = new FakeContentGenerator();
        var evidenceProvider = new FakeFocusedEvidenceProvider();
        var service = CreateService(
            patient,
            recordsRepository,
            cvRepository,
            evidenceProvider,
            contentGenerator,
            new FakeUnitOfWork());

        var result = await service.GenerateFocusedAsync(
            patient.Id,
            "  Diabetes  ");

        Assert.Equal(0, recordsRepository.GetAllConfirmedCallCount);
        Assert.Equal(1, evidenceProvider.CallCount);
        Assert.Equal("Diabetes", evidenceProvider.Focus);
        Assert.NotNull(contentGenerator.Request);
        Assert.Equal(MedicalCvScopeType.Focused, contentGenerator.Request.ScopeType);
        Assert.Equal("Diabetes", contentGenerator.Request.Focus);
        Assert.Single(contentGenerator.Request.Evidence);
        Assert.Null(cvRepository.AddedMedicalCv);
        Assert.NotNull(cvRepository.AddedVersion);
        Assert.Empty(cvRepository.AddedVersion.SummarizedRecords);
        Assert.Equal(4, result.VersionNumber);
        Assert.Equal("Diabetes", result.Focus);
    }

    [Fact]
    public async Task GenerateAsync_VersionsEachLogicalCvIndependently()
    {
        var patient = CreatePatient();
        var cvRepository = new FakeMedicalCvRepository();
        var service = CreateService(
            patient,
            new FakeMedicalRecordsRepository([]),
            cvRepository,
            new FakeFocusedEvidenceProvider(),
            new FakeContentGenerator(),
            new FakeUnitOfWork());

        var fullVersion1 = await service.GenerateFullAsync(patient.Id);
        var fullVersion2 = await service.GenerateFullAsync(patient.Id);
        var diabetesVersion1 = await service.GenerateFocusedAsync(
            patient.Id,
            "Diabetes");
        var diabetesVersion2 = await service.GenerateFocusedAsync(
            patient.Id,
            "Diabetes");
        var cardiologyVersion1 = await service.GenerateFocusedAsync(
            patient.Id,
            "Cardiology");

        Assert.Equal(fullVersion1.MedicalCvId, fullVersion2.MedicalCvId);
        Assert.Equal(1, fullVersion1.VersionNumber);
        Assert.Equal(2, fullVersion2.VersionNumber);

        Assert.Equal(
            diabetesVersion1.MedicalCvId,
            diabetesVersion2.MedicalCvId);
        Assert.Equal(1, diabetesVersion1.VersionNumber);
        Assert.Equal(2, diabetesVersion2.VersionNumber);
        Assert.NotEqual(fullVersion1.MedicalCvId, diabetesVersion1.MedicalCvId);

        Assert.Equal(1, cardiologyVersion1.VersionNumber);
        Assert.NotEqual(
            diabetesVersion1.MedicalCvId,
            cardiologyVersion1.MedicalCvId);
        Assert.Equal(3, cvRepository.MedicalCvs.Count);
        Assert.Contains(
            cvRepository.MedicalCvs,
            cv => cv.ScopeType == MedicalCvScopeType.Full && cv.Focus is null);
        Assert.Contains(
            cvRepository.MedicalCvs,
            cv => cv.ScopeType == MedicalCvScopeType.Focused &&
                  cv.Focus == "Diabetes");
        Assert.Contains(
            cvRepository.MedicalCvs,
            cv => cv.ScopeType == MedicalCvScopeType.Focused &&
                  cv.Focus == "Cardiology");
    }

    private static MedicalCvGenerationService CreateService(
        PatientProfile patient,
        FakeMedicalRecordsRepository recordsRepository,
        FakeMedicalCvRepository cvRepository,
        FakeFocusedEvidenceProvider evidenceProvider,
        FakeContentGenerator contentGenerator,
        FakeUnitOfWork unitOfWork)
    {
        return new MedicalCvGenerationService(
            new FakePatientProfileRepository(patient),
            recordsRepository,
            cvRepository,
            evidenceProvider,
            contentGenerator,
            new FakePdfGenerator(),
            new FakeFileStorage(),
            unitOfWork,
            NullLogger<MedicalCvGenerationService>.Instance);
    }

    private static PatientProfile CreatePatient()
    {
        return new PatientProfile
        {
            Id = Guid.NewGuid(),
            FullName = "Test Patient",
            BirthDate = new DateTime(1990, 3, 12),
            User = new User
            {
                Email = "patient@example.com",
                PhoneNumber = "+201000000000",
                Gender = "Female"
            }
        };
    }

    private sealed class FakePatientProfileRepository(PatientProfile patient)
        : IPatientProfileRepository
    {
        public Task<PatientProfile?> GetByIdAsync(
            Guid patientProfileId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<PatientProfile?>(
                patient.Id == patientProfileId ? patient : null);
        }

        public Task<PatientProfile?> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<PatientProfile?> UpdateByUserIdAsync(
            Guid userId,
            string? fullName,
            DateTime? birthDate,
            string? firstName,
            string? lastName,
            string? phoneNumber,
            string? gender,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeMedicalRecordsRepository(
        IReadOnlyList<MedicalRecord> records)
        : IMedicalRecordsRepository
    {
        public int GetAllConfirmedCallCount { get; private set; }

        public Task<IReadOnlyList<MedicalRecord>> GetAllConfirmedAsync(
            Guid patientProfileId,
            CancellationToken cancellationToken)
        {
            GetAllConfirmedCallCount++;
            return Task.FromResult(records);
        }

        public Task<PaginatedResult<MedicalRecord>> GetMedicalRecordsAsync(
            Guid patientProfileId,
            string? search,
            string? recordType,
            DateTime? fromDate,
            DateTime? toDate,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public void Add(MedicalRecord record) => throw new NotSupportedException();

        public Task<MedicalRecord?> GetBySourceExtractedItemIdAsync(
            Guid extractedItemId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<MedicalRecord?> GetMedicalRecordByIdAsync(
            Guid medicalRecordId,
            Guid patientProfileId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<MedicalRecord?> GetByIdWithFieldsAsync(
            Guid medicalRecordId,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class FakeMedicalCvRepository(
        MedicalCv? existingCv = null,
        int nextVersion = 1)
        : IMedicalCvRepository
    {
        private readonly List<MedicalCv> _medicalCvs = existingCv is null
            ? []
            : [existingCv];
        private readonly List<MedicalCvVersion> _versions = [];

        public IReadOnlyList<MedicalCv> MedicalCvs => _medicalCvs;
        public MedicalCv? AddedMedicalCv { get; private set; }
        public MedicalCvVersion? AddedVersion { get; private set; }

        public Task<MedicalCv?> GetByLogicalIdentityAsync(
            Guid patientId,
            MedicalCvScopeType scopeType,
            string? focus,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_medicalCvs.SingleOrDefault(cv =>
                cv.PatientId == patientId &&
                cv.ScopeType == scopeType &&
                cv.Focus == focus));
        }

        public Task<int> GetNextVersionNumberAsync(
            Guid medicalCvId,
            CancellationToken cancellationToken)
        {
            var generatedVersionCount = _versions.Count(
                version => version.MedicalCvId == medicalCvId);
            var firstVersion = existingCv?.Id == medicalCvId
                ? nextVersion
                : 1;

            return Task.FromResult(firstVersion + generatedVersionCount);
        }

        public void Add(MedicalCv medicalCv)
        {
            AddedMedicalCv = medicalCv;
            _medicalCvs.Add(medicalCv);
        }

        public void AddVersion(MedicalCvVersion version)
        {
            AddedVersion = version;
            _versions.Add(version);
        }
    }

    private sealed class FakeFocusedEvidenceProvider
        : IFocusedMedicalEvidenceProvider
    {
        public int CallCount { get; private set; }
        public string? Focus { get; private set; }

        public Task<FocusedMedicalEvidenceResponse> SearchAsync(
            Guid patientId,
            string focus,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            Focus = focus;

            return Task.FromResult(new FocusedMedicalEvidenceResponse(
                string.Empty,
                [
                    new FocusedMedicalEvidence(
                        Guid.NewGuid(),
                        0.92,
                        "Confirmed diabetes evidence",
                        "ConditionName",
                        "Diabetes")
                ]));
        }
    }

    private sealed class FakeContentGenerator : IMedicalCvContentGenerator
    {
        public MedicalCvContentRequest? Request { get; private set; }

        public Task<MedicalCvContent> GenerateAsync(
            MedicalCvContentRequest request,
            CancellationToken cancellationToken = default)
        {
            Request = request;

            return Task.FromResult(new MedicalCvContent
            {
                Title = "Medical CV",
                Summary = "Confirmed evidence summary",
                Sections = []
            });
        }
    }

    private sealed class FakePdfGenerator : IMedicalCvPdfGenerator
    {
        public byte[] Generate(MedicalCvPdfDocument document)
        {
            return "%PDF-1.7"u8.ToArray();
        }
    }

    private sealed class FakeFileStorage : IMedicalCvFileStorage
    {
        public Task<string> SaveAsync(
            byte[] pdfBytes,
            Guid medicalCvId,
            int versionNumber,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                $"medical-cvs/{medicalCvId}/version-{versionNumber}.pdf");
        }

        public Task<bool> DeleteAsync(
            string fileKey,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<int> SaveChanges()
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }

        public Task<int> SaveChanges(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }

        public Task BeginTransactionAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task CommitTransactionAsync() => Task.CompletedTask;
        public Task RollBackTransactionAsync() => Task.CompletedTask;
        public void Dispose() { }
    }
}
