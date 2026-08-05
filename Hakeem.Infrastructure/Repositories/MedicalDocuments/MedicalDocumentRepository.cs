using Hakeem.Application.Common;
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

    public Task<MedicalDocument?> GetByIdForPatientAsync(
        Guid documentId,
        Guid patientProfileId,
        CancellationToken cancellationToken)
    {
        return dbContext.MedicalDocuments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                document =>
                    document.Id == documentId &&
                    document.PatientProfileId == patientProfileId,
                cancellationToken);
    }

    public async Task<PaginatedResult<MedicalDocument>> GetDocumentsAsync(
        Guid patientProfileId,
        string? documentName,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.MedicalDocuments
            .AsNoTracking()
            .Where(document => document.PatientProfileId == patientProfileId);

        if (!string.IsNullOrWhiteSpace(documentName))
        {
            query = query.Where(document => document.Title.Contains(documentName));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(document => document.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<MedicalDocument>(
            items,
            totalCount,
            pageNumber,
            pageSize);
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
            .Include(item => item.ExtractedFields)
            .AsNoTracking()
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

    public async Task<ExtractedItem?> GetExtractedItemForReviewAsync(Guid extractedItemId,CancellationToken cancellationToken)
    {
        return await dbContext.ExtractedItems.Include(e => e.MedicalDocument).Include(e => e.ExtractedFields)
                                             .ThenInclude(f => f.FieldReview)
                                             .FirstOrDefaultAsync(
                                              e => e.Id == extractedItemId,
                                              cancellationToken);
    }
}
