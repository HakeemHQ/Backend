using Hakeem.Application.Features.MedicalDocuments.DTOs;
using Hakeem.Application.Services.DocumentExtraction;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Documents;
using Hakeem.Infrastructure.Tests.DocumentExtraction.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hakeem.Infrastructure.Tests.DocumentExtraction;

public sealed class DocumentExtractionProcessorTests
{
    [Fact]
    public async Task ProcessAsync_ValidResult_ReplacesRowsTransactionally()
    {
        var document = CreateQueuedDocument();
        var existingItem = new ExtractedItem
        {
            Id = Guid.NewGuid(),
            MedicalDocumentId = document.Id,
            ItemType = "Medication",
            SequenceNumber = 1,
            PageNumber = 1
        };
        var repository = new FakeMedicalDocumentRepository(
            document,
            [existingItem]);
        var agent = new FakeDocumentProcessingAgent(
            CreateValidResult());
        var unitOfWork = new FakeUnitOfWork();
        var processor = CreateProcessor(
            repository,
            agent,
            unitOfWork);

        await processor.ProcessAsync(
            document.Id,
            CancellationToken.None);

        Assert.Equal(
            ExtractionStatus.Completed,
            document.ExtractionStatus);
        Assert.Equal("Prescription", document.DocumentType);
        Assert.Equal(1, repository.GetExtractedItemsCallCount);
        Assert.Same(
            existingItem,
            Assert.Single(repository.RemovedItems));

        var addedItem = Assert.Single(repository.AddedItems);
        Assert.Equal("Medication", addedItem.ItemType);
        var addedField = Assert.Single(
            addedItem.ExtractedFields);
        Assert.Equal("MedicationName", addedField.FieldName);
        Assert.Null(addedField.ExtractedValue);
        Assert.Null(addedField.Confidence);
        Assert.Null(addedField.EvidenceText);
        Assert.Equal("[]", addedField.Issues);

        Assert.Equal(3, unitOfWork.SaveChangesCallCount);
        Assert.Equal(1, unitOfWork.BeginTransactionCallCount);
        Assert.Equal(1, unitOfWork.CommitTransactionCallCount);
        Assert.Equal(0, unitOfWork.RollbackTransactionCallCount);
    }

    [Fact]
    public async Task ProcessAsync_InvalidLaterItem_DoesNotTouchExtractionRows()
    {
        var document = CreateQueuedDocument();
        var repository = new FakeMedicalDocumentRepository(
            document);
        var result = new DocumentExtractionResult(
            MedicalDocumentType.Prescription,
            [
                CreateValidItem(),
                new ExtractedItemResult(
                    "UnsupportedItem",
                    1,
                    1,
                    [])
            ]);
        var agent = new FakeDocumentProcessingAgent(result);
        var unitOfWork = new FakeUnitOfWork();
        var processor = CreateProcessor(
            repository,
            agent,
            unitOfWork);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => processor.ProcessAsync(
                document.Id,
                CancellationToken.None));

        Assert.Equal(
            ExtractionStatus.Processing,
            document.ExtractionStatus);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Equal(0, unitOfWork.BeginTransactionCallCount);
        Assert.Equal(0, repository.GetExtractedItemsCallCount);
        Assert.Empty(repository.RemovedItems);
        Assert.Empty(repository.AddedItems);
    }

    [Fact]
    public async Task ProcessAsync_CompletedDocument_IsIdempotentNoOp()
    {
        var document = CreateQueuedDocument();
        document.StartExtraction();
        document.CompleteExtraction();
        var repository = new FakeMedicalDocumentRepository(
            document);
        var agent = new FakeDocumentProcessingAgent(
            CreateValidResult());
        var unitOfWork = new FakeUnitOfWork();
        var processor = CreateProcessor(
            repository,
            agent,
            unitOfWork);

        await processor.ProcessAsync(
            document.Id,
            CancellationToken.None);

        Assert.Equal(0, agent.CallCount);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
        Assert.Equal(0, repository.GetExtractedItemsCallCount);
    }

    [Fact]
    public async Task ProcessAsync_ProcessingDocument_AllowsSafeRetry()
    {
        var document = CreateQueuedDocument();
        document.StartExtraction();
        var repository = new FakeMedicalDocumentRepository(
            document);
        var agent = new FakeDocumentProcessingAgent(
            CreateValidResult());
        var unitOfWork = new FakeUnitOfWork();
        var processor = CreateProcessor(
            repository,
            agent,
            unitOfWork);

        await processor.ProcessAsync(
            document.Id,
            CancellationToken.None);

        Assert.Equal(1, agent.CallCount);
        Assert.Equal(
            ExtractionStatus.Completed,
            document.ExtractionStatus);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
        Assert.Equal(1, unitOfWork.CommitTransactionCallCount);
    }

    private static DocumentExtractionProcessor CreateProcessor(
        FakeMedicalDocumentRepository repository,
        FakeDocumentProcessingAgent agent,
        FakeUnitOfWork unitOfWork)
    {
        return new DocumentExtractionProcessor(
            repository,
            agent,
            new DocumentExtractionValidator(),
            unitOfWork,
            NullLogger<DocumentExtractionProcessor>.Instance);
    }

    private static MedicalDocument CreateQueuedDocument()
    {
        return new MedicalDocument
        {
            Id = Guid.NewGuid(),
            DocumentType = "Pending",
            Title = "Test document",
            FilePath = "TestDocuments/test.pdf"
        };
    }

    private static DocumentExtractionResult CreateValidResult()
    {
        return new DocumentExtractionResult(
            MedicalDocumentType.Prescription,
            [CreateValidItem()]);
    }

    private static ExtractedItemResult CreateValidItem()
    {
        return new ExtractedItemResult(
            "Medication",
            1,
            1,
            [
                new ExtractedFieldResult(
                    "MedicationName",
                    null,
                    null,
                    null,
                    [])
            ]);
    }
}
