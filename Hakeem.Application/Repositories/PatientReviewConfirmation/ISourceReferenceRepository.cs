using Hakeem.Application.Common;
using Hakeem.Domain.Entities;

namespace Hakeem.Application.Repositories.PatientReviewConfirmation
{
    public interface ISourceReferenceRepository
    {
        void Add(SourceReference sourceReference);

        Task<PaginatedResult<MedicalRecord>> GetMedicalRecordsByDocumentAsync(
            Guid documentId,
            string? search,
            string? recordType,
            DateTime? fromDate,
            DateTime? toDate,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken);
    }
}
