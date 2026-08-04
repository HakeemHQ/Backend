using Hakeem.Domain.Enums.Reviews;
namespace Hakeem.Application.Features.PatientReviewAndConfirmation.DTOs
{
    public class PatientReviewedFieldDTO
    {
        public Guid ExtractedFieldId { get; set; }
        public string FieldName { get; set; } = default!;
        public string? OriginalExtractedValue { get; set; }
        public FieldReviewDecision Decision { get; set; }
        public string? CorrectedValue { get; set; }
        public string? ConfirmedValue { get; set; }
    }
}
