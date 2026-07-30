using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Repositories.MedicalDocuments;

public interface IMedicalDocumentRepository : IScoped
{
    void Add(MedicalDocument medicalDocument);

    Task<MedicalDocument?> GetByIdAsync(
        Guid documentId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ExtractedItem>> GetExtractedItemsAsync(
        Guid documentId,
        CancellationToken cancellationToken);

    void RemoveExtractedItems(
        IEnumerable<ExtractedItem> extractedItems);

    void AddExtractedItems(
        IEnumerable<ExtractedItem> extractedItems);
}
