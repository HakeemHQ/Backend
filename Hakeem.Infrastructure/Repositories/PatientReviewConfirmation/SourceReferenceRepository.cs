using Hakeem.Application.Common;
using Hakeem.Application.Repositories.PatientReviewConfirmation;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces.ServiceLifetime;
using Hakeem.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Hakeem.Infrastructure.Repositories.PatientReviewConfirmation
{
    public class SourceReferenceRepository : ISourceReferenceRepository, IScoped
    {
        private readonly ApplicationDbContext _context;

        public SourceReferenceRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public void Add(SourceReference sourceReference)
        {
            _context.SourceReferences.Add(sourceReference);
        }

        public async Task<PaginatedResult<MedicalRecord>> GetMedicalRecordsByDocumentAsync(
            Guid documentId,
            string? search,
            string? recordType,
            DateTime? fromDate,
            DateTime? toDate,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
        {
            IQueryable<MedicalRecord> query = _context.MedicalRecords
                .AsNoTracking()
                .Where(record => record.SourceReferences.Any(
                    reference => reference.DocumentId == documentId));

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(record => record.DisplayName.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(recordType))
            {
                query = query.Where(record => record.RecordType == recordType);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(record => record.ClinicalDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(record => record.ClinicalDate <= toDate.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(record => record.ClinicalDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PaginatedResult<MedicalRecord>(
                items,
                totalCount,
                pageNumber,
                pageSize);
        }
    }

}
