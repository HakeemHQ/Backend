using Hakeem.Application.Features.PatientReviewAndConfirmation.DTOs;
using Hakeem.Domain.Enums.Reviews;
using MediatR;

namespace Hakeem.Application.Features.PatientReviewAndConfirmation.Commands
{
    public class PatientReviewConfirmationCommand : IRequest<ReviewExtractedItemResult>
    {
        public Guid ExtractedItemId { get; set; }
        public List<PatientReviewConfirmDTO> Fields { get; set; } = new();
    }
    
    public sealed record ReviewExtractedItemResult(Guid ExtractedItemId,string ItemType,string ReviewStatus,
                         string DocumentReviewStatus,Guid? MedicalRecordId,DateTimeOffset ReviewedAt,
                         IReadOnlyCollection<ReviewedFieldResult> Fields);

    public sealed record ReviewedFieldResult(Guid ExtractedFieldId,string FieldName,string? OriginalExtractedValue,
                                             FieldReviewDecision Decision,string? CorrectedValue,string? ConfirmedValue);
}
