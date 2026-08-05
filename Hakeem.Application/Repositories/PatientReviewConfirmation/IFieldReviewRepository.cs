using Hakeem.Domain.Entities;

namespace Hakeem.Application.Repositories.PatientReviewConfirmation
{
    public interface  IFieldReviewRepository
    {
        void Add(FieldReview review);

        Task<FieldReview?> GetByExtractedFieldIdAsync(Guid extractedFieldId,CancellationToken cancellationToken);
    }
}
