using Hakeem.Domain.Enums.Reviews;
namespace Hakeem.Application.Features.PatientReviewAndConfirmation.DTOs
{
    public class PatientReviewConfirmDTO
    {
        public Guid ExtractedFieldId { get; set; }

        public FieldReviewDecision Decision { get; set; }

        public string? CorrectedValue { get; set; }
    }
}
