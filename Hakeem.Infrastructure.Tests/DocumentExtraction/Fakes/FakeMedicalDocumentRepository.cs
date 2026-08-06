using Hakeem.Application.Common;
using Hakeem.Application.Repositories.MedicalDocuments;
using Hakeem.Domain.Entities;

namespace Hakeem.Infrastructure.Tests.DocumentExtraction.Fakes;

internal sealed class FakeMedicalDocumentRepository(
    MedicalDocument? medicalDocument,
    IReadOnlyList<ExtractedItem>? existingItems = null)
    : IMedicalDocumentRepository 
{
    private readonly IReadOnlyList<ExtractedItem> _existingItems =
        existingItems ?? [];

    public int GetExtractedItemsCallCount { get; private set; }
    public IReadOnlyList<ExtractedItem> RemovedItems { get; private set; } = [];
    public IReadOnlyList<ExtractedItem> AddedItems { get; private set; } = [];

    public void Add(MedicalDocument entity)
    {
        throw new NotSupportedException();
    }

    public Task<MedicalDocument?> GetByIdAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(medicalDocument);
    }

    public Task<MedicalDocument?> GetByIdForPatientAsync(
        Guid documentId,
        Guid patientProfileId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (medicalDocument is null ||
            medicalDocument.Id != documentId ||
            medicalDocument.PatientProfileId != patientProfileId)
        {
            return Task.FromResult<MedicalDocument?>(null);
        }

        return Task.FromResult<MedicalDocument?>(medicalDocument);
    }

    public Task<PaginatedResult<MedicalDocument>> GetDocumentsAsync(
        Guid patientProfileId,
        string? documentName,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (medicalDocument is null || medicalDocument.PatientProfileId != patientProfileId)
        {
            return Task.FromResult(new PaginatedResult<MedicalDocument>(
                [],
                0,
                pageNumber,
                pageSize));
        }

        var matchesName = string.IsNullOrWhiteSpace(documentName) ||
                          medicalDocument.Title.Contains(documentName, StringComparison.OrdinalIgnoreCase);

        var items = matchesName ? new[] { medicalDocument } : Array.Empty<MedicalDocument>();

        return Task.FromResult(new PaginatedResult<MedicalDocument>(
            items,
            items.Length,
            pageNumber,
            pageSize));
    }

    public Task<IReadOnlyList<ExtractedItem>>
        GetExtractedItemsAsync(
            Guid documentId,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        GetExtractedItemsCallCount++;

        return Task.FromResult(_existingItems);
    }

    public void RemoveExtractedItems(
        IEnumerable<ExtractedItem> extractedItems)
    {
        RemovedItems = extractedItems.ToList();
    }

    public void AddExtractedItems(
        IEnumerable<ExtractedItem> extractedItems)
    {
        AddedItems = extractedItems.ToList();
    }

    public Task<ExtractedItem?> GetExtractedItemForReviewAsync(Guid extractedItemId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
