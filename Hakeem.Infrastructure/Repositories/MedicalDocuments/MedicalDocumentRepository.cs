using Hakeem.Application.Repositories.MedicalDocuments;
using Hakeem.Domain.Entities;
using Hakeem.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Hakeem.Infrastructure.Repositories.MedicalDocuments;

public sealed class MedicalDocumentRepository(ApplicationDbContext dbContext)
    : IMedicalDocumentRepository
{
    public void Add(MedicalDocument medicalDocument)
    {
        dbContext.MedicalDocuments.Add(medicalDocument);
    }

    public Task<MedicalDocument?> GetByIdAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        return dbContext.MedicalDocuments.SingleOrDefaultAsync(
            document => document.Id == documentId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ExtractedItem>>
        GetExtractedItemsAsync(
            Guid documentId,
            CancellationToken cancellationToken)
    {
        return await dbContext.ExtractedItems
            .Where(
                item =>
                    item.MedicalDocumentId == documentId)
            .ToListAsync(cancellationToken);
    }

    public void RemoveExtractedItems(
        IEnumerable<ExtractedItem> extractedItems)
    {
        dbContext.ExtractedItems.RemoveRange(extractedItems);
    }

    public void AddExtractedItems(
        IEnumerable<ExtractedItem> extractedItems)
    {
        dbContext.ExtractedItems.AddRange(extractedItems);
    }
}
