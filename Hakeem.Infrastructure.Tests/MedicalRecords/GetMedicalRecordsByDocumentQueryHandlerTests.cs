using Hakeem.Application.Common;
using Hakeem.Application.Features.Doctor.MedicalRecords.Queries.GetMedicalRecordsByDocument;
using Hakeem.Application.Repositories.PatientReviewConfirmation;
using Hakeem.Domain.Entities;

namespace Hakeem.Infrastructure.Tests.MedicalRecords;

public sealed class GetMedicalRecordsByDocumentQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMappedPaginatedRecordsAndForwardsFilters()
    {
        var documentId = Guid.NewGuid();
        var clinicalDate = new DateTime(2026, 8, 10, 9, 30, 0, DateTimeKind.Utc);
        var repository = new FakeSourceReferenceRepository(
            new MedicalRecord
            {
                Id = Guid.NewGuid(),
                RecordType = "Diagnosis",
                DisplayName = "Hypertension",
                ClinicalDate = clinicalDate,
                Status = "Confirmed"
            });
        var handler = new GetMedicalRecordsByDocumentQueryHandler(repository);
        var fromDate = clinicalDate.AddDays(-1);
        var toDate = clinicalDate.AddDays(1);

        var result = await handler.Handle(
            new GetMedicalRecordsByDocumentQuery(
                documentId,
                "hyper",
                "Diagnosis",
                fromDate,
                toDate,
                PageNumber: 2,
                PageSize: 5),
            CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(repository.Record.Id, item.MedicalRecordId);
        Assert.Equal(repository.Record.RecordType, item.RecordType);
        Assert.Equal(repository.Record.DisplayName, item.DisplayName);
        Assert.Equal(repository.Record.ClinicalDate, item.ClinicalDate);
        Assert.Equal(repository.Record.Status, item.Status);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(documentId, repository.DocumentId);
        Assert.Equal("hyper", repository.Search);
        Assert.Equal("Diagnosis", repository.RecordType);
        Assert.Equal(fromDate, repository.FromDate);
        Assert.Equal(toDate, repository.ToDate);
    }

    private sealed class FakeSourceReferenceRepository(MedicalRecord record)
        : ISourceReferenceRepository
    {
        public MedicalRecord Record { get; } = record;
        public Guid DocumentId { get; private set; }
        public string? Search { get; private set; }
        public string? RecordType { get; private set; }
        public DateTime? FromDate { get; private set; }
        public DateTime? ToDate { get; private set; }

        public void Add(SourceReference sourceReference) =>
            throw new NotSupportedException();

        public Task<PaginatedResult<MedicalRecord>> GetMedicalRecordsByDocumentAsync(
            Guid documentId,
            string? search,
            string? recordType,
            DateTime? fromDate,
            DateTime? toDate,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
        {
            DocumentId = documentId;
            Search = search;
            RecordType = recordType;
            FromDate = fromDate;
            ToDate = toDate;

            return Task.FromResult(new PaginatedResult<MedicalRecord>(
                [Record],
                1,
                pageNumber,
                pageSize));
        }
    }
}
