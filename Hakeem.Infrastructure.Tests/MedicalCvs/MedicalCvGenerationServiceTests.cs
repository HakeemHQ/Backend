using Hakeem.Application.Common;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.MedicalCvs.DTOs;
using Hakeem.Application.Interfaces.MedicalCvs;
using Hakeem.Application.Repositories.MedicalCvs;
using Hakeem.Application.Repositories.MedicalRecords;
using Hakeem.Application.Repositories.Notifications;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Application.Services.MedicalCvs;
using Hakeem.Domain.DomainEvents.Outbox;
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
        var unitOfWork = new FakeUnitOfWork();
        var outboxRepository = new FakeOutboxEventRepository();
        var service = CreateService(
            patient,
            recordsRepository,
            cvRepository,
            contentGenerator,
            unitOfWork,
            outboxRepository);

        var result = await service.GenerateFullAsync(
            patient.Id,
            "Mazen Medical CV",
            "ar");

        Assert.Equal(1, recordsRepository.GetAllConfirmedCallCount);
        Assert.Equal(0, contentGenerator.CallCount);
        Assert.NotNull(cvRepository.AddedMedicalCv);
        Assert.Equal("Mazen Medical CV", cvRepository.AddedMedicalCv.Title);
        Assert.Equal(MedicalCvScopeType.Full, cvRepository.AddedMedicalCv.ScopeType);
        Assert.Null(cvRepository.AddedMedicalCv.Focus);
        Assert.NotNull(cvRepository.AddedVersion);
        Assert.Equal(MedicalCvVersionStatus.Queued, cvRepository.AddedVersion.Status);
        Assert.Empty(cvRepository.AddedVersion.PdfFileKey);
        Assert.Same(
            confirmedRecord,
            Assert.Single(cvRepository.AddedVersion.SummarizedRecords));
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        var queuedEvent = Assert.IsType<CreateMedicalCvRequest>(
            outboxRepository.AddedEvent);
        Assert.Equal(result.MedicalCvVersionId, queuedEvent.MedicalCvVersionId);
        Assert.Equal("ar", queuedEvent.Language);
        Assert.Equal(
            $"medical-cv-generation:{result.MedicalCvVersionId}",
            outboxRepository.IdempotencyKey);
        Assert.Equal(MedicalCvVersionStatus.Queued, result.Status);
        Assert.Empty(result.PdfFileKey);
    }

    [Fact]
    public async Task GenerateFocusedAsync_UsesOnlyFocusedEvidenceAndUpdatesRequestedTitle()
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
        var service = CreateService(
            patient,
            recordsRepository,
            cvRepository,
            contentGenerator,
            new FakeUnitOfWork());

        var result = await service.GenerateFocusedAsync(
            patient.Id,
            "  Diabetes  ",
            "Ignored Replacement Title",
            CreateEvidence(),
            "en");

        Assert.Equal(0, recordsRepository.GetAllConfirmedCallCount);
        Assert.NotNull(contentGenerator.Request);
        Assert.Equal(MedicalCvScopeType.Focused, contentGenerator.Request.ScopeType);
        Assert.Equal("Diabetes", contentGenerator.Request.Focus);
        Assert.Equal("Ignored Replacement Title", contentGenerator.Request.Title);
        Assert.Equal("Ignored Replacement Title", existingCv.Title);
        Assert.Single(contentGenerator.Request.Evidence);
        Assert.Null(cvRepository.AddedMedicalCv);
        Assert.NotNull(cvRepository.AddedVersion);
        Assert.Empty(cvRepository.AddedVersion.SummarizedRecords);
        Assert.Equal(4, result.VersionNumber);
        Assert.Equal("Diabetes", result.Focus);
        Assert.Equal("Ignored Replacement Title", result.Title);
    }

    [Fact]
    public async Task GenerateAsync_VersionsEachLogicalCvIndependently()
    {
        var patient = CreatePatient();
        var cvRepository = new FakeMedicalCvRepository();
        var service = CreateService(
            patient,
            new FakeMedicalRecordsRepository(
            [
                new MedicalRecord
                {
                    Id = Guid.NewGuid(),
                    PatientProfileId = patient.Id,
                    DisplayName = "Confirmed medical record",
                    Status = "Confirmed"
                }
            ]),
            cvRepository,
            new FakeContentGenerator(),
            new FakeUnitOfWork());

        var fullVersion1 = await service.GenerateFullAsync(
            patient.Id,
            "Initial Full CV",
            "en");
        var fullVersion2 = await service.GenerateFullAsync(
            patient.Id,
            "Ignored Replacement Title",
            "en");
        var diabetesVersion1 = await service.GenerateFocusedAsync(
            patient.Id,
            "Diabetes",
            "Diabetes Medical CV",
            CreateEvidence(),
            "en");
        var diabetesVersion2 = await service.GenerateFocusedAsync(
            patient.Id,
            "Diabetes",
            "Ignored Replacement Title",
            CreateEvidence(),
            "en");
        var cardiologyVersion1 = await service.GenerateFocusedAsync(
            patient.Id,
            "Cardiology",
            "Cardiology Medical CV",
            CreateEvidence(),
            "en");

        Assert.Equal(fullVersion1.MedicalCvId, fullVersion2.MedicalCvId);
        Assert.Equal("Initial Full CV", fullVersion2.Title);
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

    [Fact]
    public async Task GenerateFullAsync_WithoutSuitableConfirmedRecords_Returns422Error()
    {
        var patient = CreatePatient();
        var contentGenerator = new FakeContentGenerator();
        var service = CreateService(
            patient,
            new FakeMedicalRecordsRepository(
            [
                new MedicalRecord
                {
                    Id = Guid.NewGuid(),
                    PatientProfileId = patient.Id,
                    DisplayName = "   ",
                    Status = "Confirmed"
                }
            ]),
            new FakeMedicalCvRepository(),
            contentGenerator,
            new FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<UnprocessableEntityException>(
            () => service.GenerateFullAsync(patient.Id, "Medical CV", "en"));

        Assert.Equal(
            ErrorCodes.MedicalCvNoConfirmedInformation,
            exception.ErrorCode);
        Assert.Equal(0, contentGenerator.CallCount);
    }

    private static MedicalCvGenerationService CreateService(
        PatientProfile patient,
        FakeMedicalRecordsRepository recordsRepository,
        FakeMedicalCvRepository cvRepository,
        FakeContentGenerator contentGenerator,
        FakeUnitOfWork unitOfWork,
        FakeOutboxEventRepository? outboxEventRepository = null)
    {
        return new MedicalCvGenerationService(
            new FakePatientProfileRepository(patient),
            recordsRepository,
            cvRepository,
            contentGenerator,
            new FakePdfGenerator(),
            new FakeFileStorage(),
            outboxEventRepository ?? new FakeOutboxEventRepository(),
            unitOfWork,
            NullLogger<MedicalCvGenerationService>.Instance);
    }

    private static IReadOnlyList<MedicalCvEvidenceItem> CreateEvidence() =>
    [
        new MedicalCvEvidenceItem(
            Guid.NewGuid(),
            0.92,
            "Confirmed focused medical evidence",
            "Condition",
            "2026-08-06T00:00:00.0000000Z")
    ];

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

        public Task<IReadOnlyList<MedicalRecord>>
            GetConfirmedByRecordTypeAsync(
                Guid patientProfileId,
                string recordType,
                CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MedicalRecord>>(
                records.Where(record => string.Equals(
                    record.RecordType,
                    recordType,
                    StringComparison.OrdinalIgnoreCase)).ToArray());

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

        public Task<MedicalRecord?> GetByIdWithDetailsAsync(
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

        public Task<MedicalCvVersion?> GetVersionForPatientAsync(
            Guid medicalCvVersionId,
            Guid patientId,
            CancellationToken cancellationToken)
        {
            var version = _versions.SingleOrDefault(item =>
                item.Id == medicalCvVersionId);
            var belongsToPatient = _medicalCvs.Any(cv =>
                cv.Id == version?.MedicalCvId &&
                cv.PatientId == patientId);

            return Task.FromResult(
                belongsToPatient ? version : null);
        }

        public Task<MedicalCvVersion?> GetVersionForGenerationAsync(
            Guid medicalCvVersionId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_versions.SingleOrDefault(
                version => version.Id == medicalCvVersionId));

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

        public Task<IReadOnlyList<MedicalCv>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IMedicalCvRepository.MedicalCvVersionReadModel?> GetVersionByIdAsync(Guid medicalCvVersionId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<MedicalCvReadModel?> GetByIdAsync(Guid medicalCvId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<MedicalCvVersion?> GetVersionForApprovalAsync(Guid medicalCvVersionId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class FakeContentGenerator : IMedicalCvContentGenerator
    {
        public int CallCount { get; private set; }
        public MedicalCvContentRequest? Request { get; private set; }

        public Task<MedicalCvContent> GenerateAsync(
            MedicalCvContentRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
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

        public Task<Stream> OpenReadAsync(
            string fileKey,
            CancellationToken cancellationToken = default)
        {
            Stream stream = new MemoryStream("%PDF-1.7"u8.ToArray());
            return Task.FromResult(stream);
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

    private sealed class FakeOutboxEventRepository : IOutboxEventRepository
    {
        public OutboxEventBase? AddedEvent { get; private set; }
        public string? IdempotencyKey { get; private set; }

        public void Add<TEvent>(TEvent @event, string? idempotencyKey = null)
            where TEvent : OutboxEventBase
        {
            AddedEvent = @event;
            IdempotencyKey = idempotencyKey;
        }

        public Task<bool> ExistsByIdempotencyKeyAsync(
            string key,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task SaveAsync(
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
