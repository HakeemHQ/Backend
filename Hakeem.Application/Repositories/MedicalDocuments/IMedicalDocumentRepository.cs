using Hakeem.Application.Common;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Repositories.MedicalDocuments;

public interface IMedicalDocumentRepository : IScoped
{
    void Add(MedicalDocument medicalDocument);

    void Remove(MedicalDocument medicalDocument);

    Task<bool> HasSourceReferencesAsync(
        Guid documentId,
        CancellationToken cancellationToken);

    Task<MedicalDocument?> GetByIdAsync(Guid documentId, CancellationToken cancellationToken);

    Task<MedicalDocument?> GetByIdForPatientAsync(
        Guid documentId,
        Guid patientProfileId,
        CancellationToken cancellationToken);

    Task<PaginatedResult<MedicalDocument>> GetDocumentsAsync(
        Guid patientProfileId,
        string? documentName,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ExtractedItem>> GetExtractedItemsAsync(Guid documentId,CancellationToken cancellationToken);

    void RemoveExtractedItems(IEnumerable<ExtractedItem> extractedItems);

    void AddExtractedItems(IEnumerable<ExtractedItem> extractedItems);
    Task<ExtractedItem?> GetExtractedItemForReviewAsync(Guid extractedItemId,CancellationToken cancellationToken);
}
