using Hakeem.Application.Common;
using Hakeem.Application.Repositories.MedicalRecords;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces.ServiceLifetime;
using Hakeem.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Infrastructure.Repositories.MedicalRecords
{
    public class MedicalRecordsRepository(ApplicationDbContext _context) : IMedicalRecordsRepository,IScoped
    {
        public async Task<PaginatedResult<MedicalRecord>> GetMedicalRecordsAsync(
         Guid patientProfileId,
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
                .Where(x => x.PatientProfileId == patientProfileId);

            // Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.DisplayName.Contains(search));
            }

            // Filter by record type
            if (!string.IsNullOrWhiteSpace(recordType))
            {
                query = query.Where(x =>
                    x.RecordType == recordType);
            }

            // Filter by date
            if (fromDate.HasValue)
            {
                query = query.Where(x =>
                    x.ClinicalDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(x =>
                    x.ClinicalDate <= toDate.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.ClinicalDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PaginatedResult<MedicalRecord>(items,totalCount,pageNumber,pageSize);
        }

        public void Add(MedicalRecord record)
        {
            _context.MedicalRecords.Add(record);
        }

        public Task<MedicalRecord?> GetBySourceExtractedItemIdAsync(Guid extractedItemId,CancellationToken cancellationToken)
        {
            return _context.MedicalRecords.Include(x => x.Fields).FirstOrDefaultAsync(x =>
                                                                 x.SourceExtractedItemId == extractedItemId,
                                                                 cancellationToken);
        }
        

        public async Task<MedicalRecord?> GetMedicalRecordByIdAsync(Guid medicalRecordId,Guid patientProfileId,
                                                                    CancellationToken cancellationToken)
        {
            return await _context.MedicalRecords.AsNoTracking().Include(x => x.Fields)
                        .Include(x => x.SourceReferences).ThenInclude(x => x.MedicalDocument)
                        .FirstOrDefaultAsync(x => x.Id == medicalRecordId &&
                                             x.PatientProfileId == patientProfileId,
                                             cancellationToken);

        }

        public Task<MedicalRecord?> GetByIdWithFieldsAsync(
            Guid medicalRecordId,
            CancellationToken cancellationToken)
        {
            return _context.MedicalRecords
                .AsNoTracking()
                .Include(record => record.Fields)
                .SingleOrDefaultAsync(
                    record => record.Id == medicalRecordId,
                    cancellationToken);
        }

        public async Task<IReadOnlyList<MedicalRecord>> GetAllConfirmedAsync(
            Guid patientProfileId,
            CancellationToken cancellationToken)
        {
            return await _context.MedicalRecords
                .Where(record =>
                    record.PatientProfileId == patientProfileId &&
                    record.Status == "Confirmed")
                .OrderByDescending(record => record.ClinicalDate)
                .ToListAsync(cancellationToken);
        }
    }
}

