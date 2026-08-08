using Hakeem.Application.Common;
using Hakeem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Repositories.MedicalRecords
{
    public interface IMedicalRecordsRepository
    {
        Task<PaginatedResult<MedicalRecord>> GetMedicalRecordsAsync(Guid patientProfileId,string? search,string? recordType,
                                                                    DateTime? fromDate,DateTime? toDate, int pageNumber,
                                                                    int pageSize,CancellationToken cancellationToken);
        void Add(MedicalRecord record);
        Task<MedicalRecord?> GetBySourceExtractedItemIdAsync(Guid extractedItemId,CancellationToken cancellationToken);
        Task<MedicalRecord?> GetMedicalRecordByIdAsync(Guid medicalRecordId,Guid patientProfileId,CancellationToken cancellationToken);

        Task<MedicalRecord?> GetByIdWithFieldsAsync(
            Guid medicalRecordId,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<MedicalRecord>> GetAllConfirmedAsync(
            Guid patientProfileId,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<MedicalRecord>> GetConfirmedByRecordTypeAsync(
            Guid patientProfileId,
            string recordType,
            CancellationToken cancellationToken);
    }
}
