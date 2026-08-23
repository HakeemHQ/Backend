using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Features.PatientReviewAndConfirmation.Commands;
using Hakeem.Application.Features.PatientReviewAndConfirmation.DTOs;
using Hakeem.Application.Interfaces.Rag;
using Hakeem.Application.Repositories.MedicalRecords;
using Hakeem.Application.Repositories.PatientReviewConfirmation;
using Hakeem.Application.Services.Access;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Documents;
using Hakeem.Domain.Enums.Reviews;
using Hakeem.Infrastructure.Tests.DocumentExtraction.Fakes;

namespace Hakeem.Infrastructure.Tests.PatientReview;

public sealed class PatientReviewConfirmationCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenAllFieldsRejected_DoesNotCreateMedicalRecord_AndReturnsNullMedicalRecordId()
    {
        var patientId = Guid.NewGuid();
        var document = CreateCompletedDocument(patientId);
        var item = CreateItem(document.Id, "BloodPressure", "120/80", "HeartRate", "72");
        item.MedicalDocument = document;
        document.ExtractedItems.Add(item);

        var currentUserId = Guid.NewGuid();
        var fakeUserContext = new FakeCurrentUserContext(currentUserId);
        var fakeGuard = new FakeDoctorPatientAccessGuard();
        var fakeDocRepo = new FakeMedicalDocumentRepository(document, [item]);
        var fakeRecordsRepo = new FakeMedicalRecordsRepository();
        var fakeFieldReviewRepo = new FakeFieldReviewRepository();
        var fakeSourceRefRepo = new FakeSourceReferenceRepository();
        var fakeIndexOutbox = new FakeMedicalRecordIndexOutbox();
        var fakeUnitOfWork = new FakeUnitOfWork();

        var handler = new PatientReviewConfirmationCommandHandler(
            fakeUserContext,
            fakeGuard,
            fakeDocRepo,
            fakeRecordsRepo,
            fakeFieldReviewRepo,
            fakeSourceRefRepo,
            fakeIndexOutbox,
            fakeUnitOfWork);

        var command = new PatientReviewConfirmationCommand
        {
            ExtractedItemId = item.Id,
            Fields = item.ExtractedFields.Select(f => new PatientReviewConfirmDTO
            {
                ExtractedFieldId = f.Id,
                Decision = FieldReviewDecision.Rejected
            }).ToList()
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Null(result.MedicalRecordId);
        Assert.Empty(fakeRecordsRepo.AddedRecords);
        Assert.Empty(fakeIndexOutbox.EnqueuedIds);
        Assert.Empty(fakeSourceRefRepo.AddedReferences);
        Assert.Equal(ExtractedItemReviewStatus.Reviewed.ToString(), result.ReviewStatus);
        Assert.Equal(DocumentReviewStatus.FullyReviewed.ToString(), result.DocumentReviewStatus);
        Assert.Equal(1, fakeUnitOfWork.SaveChangesCallCount);
        Assert.All(result.Fields, f =>
        {
            Assert.Equal(FieldReviewDecision.Rejected, f.Decision);
            Assert.Null(f.ConfirmedValue);
        });
    }

    [Fact]
    public async Task Handle_WhenAllFieldsApproved_CreatesMedicalRecord_AndReturnsMedicalRecordId()
    {
        var patientId = Guid.NewGuid();
        var document = CreateCompletedDocument(patientId);
        var item = CreateItem(document.Id, "BloodPressure", "120/80", "HeartRate", "72");
        item.MedicalDocument = document;
        document.ExtractedItems.Add(item);

        var currentUserId = Guid.NewGuid();
        var fakeUserContext = new FakeCurrentUserContext(currentUserId);
        var fakeGuard = new FakeDoctorPatientAccessGuard();
        var fakeDocRepo = new FakeMedicalDocumentRepository(document, [item]);
        var fakeRecordsRepo = new FakeMedicalRecordsRepository();
        var fakeFieldReviewRepo = new FakeFieldReviewRepository();
        var fakeSourceRefRepo = new FakeSourceReferenceRepository();
        var fakeIndexOutbox = new FakeMedicalRecordIndexOutbox();
        var fakeUnitOfWork = new FakeUnitOfWork();

        var handler = new PatientReviewConfirmationCommandHandler(
            fakeUserContext,
            fakeGuard,
            fakeDocRepo,
            fakeRecordsRepo,
            fakeFieldReviewRepo,
            fakeSourceRefRepo,
            fakeIndexOutbox,
            fakeUnitOfWork);

        var command = new PatientReviewConfirmationCommand
        {
            ExtractedItemId = item.Id,
            Fields = item.ExtractedFields.Select(f => new PatientReviewConfirmDTO
            {
                ExtractedFieldId = f.Id,
                Decision = FieldReviewDecision.Approved
            }).ToList()
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result.MedicalRecordId);
        var createdRecord = Assert.Single(fakeRecordsRepo.AddedRecords);
        Assert.Equal(result.MedicalRecordId, createdRecord.Id);
        Assert.Equal(2, createdRecord.Fields.Count);
        Assert.Contains("BloodPressure: 120/80", createdRecord.DisplayName);
        Assert.Contains("HeartRate: 72", createdRecord.DisplayName);
        Assert.Single(fakeIndexOutbox.EnqueuedIds, createdRecord.Id);
        var sourceRef = Assert.Single(fakeSourceRefRepo.AddedReferences);
        Assert.Equal(createdRecord.Id, sourceRef.MedicalRecordId);
        Assert.Equal(document.Id, sourceRef.DocumentId);
    }

    [Fact]
    public async Task Handle_WhenSomeFieldsRejectedAndSomeApprovedOrCorrected_CreatesRecordWithOnlyAcceptedFields()
    {
        var patientId = Guid.NewGuid();
        var document = CreateCompletedDocument(patientId);
        var item = CreateItem(document.Id, "Field1", "Value1", "Field2", "Value2", "Field3", "Value3");
        item.MedicalDocument = document;
        document.ExtractedItems.Add(item);

        var fieldsList = item.ExtractedFields.ToList();
        var field1 = fieldsList[0];
        var field2 = fieldsList[1];
        var field3 = fieldsList[2];

        var currentUserId = Guid.NewGuid();
        var fakeUserContext = new FakeCurrentUserContext(currentUserId);
        var fakeGuard = new FakeDoctorPatientAccessGuard();
        var fakeDocRepo = new FakeMedicalDocumentRepository(document, [item]);
        var fakeRecordsRepo = new FakeMedicalRecordsRepository();
        var fakeFieldReviewRepo = new FakeFieldReviewRepository();
        var fakeSourceRefRepo = new FakeSourceReferenceRepository();
        var fakeIndexOutbox = new FakeMedicalRecordIndexOutbox();
        var fakeUnitOfWork = new FakeUnitOfWork();

        var handler = new PatientReviewConfirmationCommandHandler(
            fakeUserContext,
            fakeGuard,
            fakeDocRepo,
            fakeRecordsRepo,
            fakeFieldReviewRepo,
            fakeSourceRefRepo,
            fakeIndexOutbox,
            fakeUnitOfWork);

        var command = new PatientReviewConfirmationCommand
        {
            ExtractedItemId = item.Id,
            Fields = new List<PatientReviewConfirmDTO>
            {
                new() { ExtractedFieldId = field1.Id, Decision = FieldReviewDecision.Rejected },
                new() { ExtractedFieldId = field2.Id, Decision = FieldReviewDecision.Approved },
                new() { ExtractedFieldId = field3.Id, Decision = FieldReviewDecision.Corrected, CorrectedValue = "CorrectedValue3" }
            }
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result.MedicalRecordId);
        var createdRecord = Assert.Single(fakeRecordsRepo.AddedRecords);
        Assert.Equal(result.MedicalRecordId, createdRecord.Id);
        Assert.Equal(2, createdRecord.Fields.Count);
        Assert.DoesNotContain("Field1", createdRecord.DisplayName);
        Assert.Contains("Field2: Value2", createdRecord.DisplayName);
        Assert.Contains("Field3: CorrectedValue3", createdRecord.DisplayName);
        Assert.Single(fakeIndexOutbox.EnqueuedIds, createdRecord.Id);
    }

    private static MedicalDocument CreateCompletedDocument(Guid patientId)
    {
        var document = new MedicalDocument
        {
            Id = Guid.NewGuid(),
            PatientProfileId = patientId
        };
        document.StartExtraction();
        document.CompleteExtraction();
        return document;
    }

    private static ExtractedItem CreateItem(
        Guid documentId,
        params string[] nameValuePairs)
    {
        var item = new ExtractedItem
        {
            Id = Guid.NewGuid(),
            MedicalDocumentId = documentId,
            ItemType = "VitalSigns",
            SequenceNumber = 1,
            PageNumber = 1,
            ReviewStatus = ExtractedItemReviewStatus.NotReviewed
        };

        for (int i = 0; i < nameValuePairs.Length; i += 2)
        {
            var fieldName = nameValuePairs[i];
            var fieldValue = i + 1 < nameValuePairs.Length ? nameValuePairs[i + 1] : "Val";
            item.ExtractedFields.Add(new ExtractedField
            {
                Id = Guid.NewGuid(),
                ExtractedItemId = item.Id,
                FieldName = fieldName,
                ExtractedValue = fieldValue
            });
        }

        return item;
    }

    private sealed record FakeCurrentUserContext(Guid UserId) : ICurrentUserContext;

    private sealed class FakeDoctorPatientAccessGuard : IDoctorPatientAccessGuard
    {
        public Task<DoctorProfile> RequireDoctorWithActiveAccessAsync(
            Guid patientProfileId,
            CancellationToken cancellationToken) =>
            Task.FromResult(new DoctorProfile
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid()
            });
    }

    private sealed class FakeMedicalRecordsRepository : IMedicalRecordsRepository
    {
        public List<MedicalRecord> AddedRecords { get; } = [];

        public void Add(MedicalRecord record) => AddedRecords.Add(record);

        public Task<IReadOnlyList<MedicalRecord>> GetAllConfirmedAsync(
            Guid patientProfileId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MedicalRecord>>([]);

        public Task<IReadOnlyList<MedicalRecord>> GetConfirmedByRecordTypeAsync(
            Guid patientProfileId,
            string recordType,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MedicalRecord>>([]);

        public Task<Hakeem.Application.Common.PaginatedResult<MedicalRecord>> GetMedicalRecordsAsync(
            Guid patientProfileId,
            string? search,
            string? recordType,
            DateTime? fromDate,
            DateTime? toDate,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken) =>
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

    private sealed class FakeFieldReviewRepository : IFieldReviewRepository
    {
        public List<FieldReview> AddedReviews { get; } = [];

        public void Add(FieldReview review) => AddedReviews.Add(review);

        public Task<FieldReview?> GetByExtractedFieldIdAsync(
            Guid extractedFieldId,
            CancellationToken cancellationToken) =>
            Task.FromResult<FieldReview?>(null);
    }

    private sealed class FakeSourceReferenceRepository : ISourceReferenceRepository
    {
        public List<SourceReference> AddedReferences { get; } = [];

        public void Add(SourceReference sourceReference) => AddedReferences.Add(sourceReference);

        public Task<Hakeem.Application.Common.PaginatedResult<MedicalRecord>> GetMedicalRecordsByDocumentAsync(
            Guid documentId,
            string? search,
            string? recordType,
            DateTime? fromDate,
            DateTime? toDate,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeMedicalRecordIndexOutbox : IMedicalRecordIndexOutbox
    {
        public List<Guid> EnqueuedIds { get; } = [];

        public void EnqueueIndexing(Guid medicalRecordId) => EnqueuedIds.Add(medicalRecordId);
    }
}
