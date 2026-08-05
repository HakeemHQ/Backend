using Hakeem.Domain.Enums.Reviews;
using System;

namespace Hakeem.Domain.Entities;

public class FieldReview : BaseEntity
{
    public Guid ExtractedFieldId { get; set; }
    public FieldReviewDecision Decision { get; set; }
    public string? CorrectedValue { get; set; }
    public DateTimeOffset ReviewedAt { get; set; }
    public virtual ExtractedField ExtractedField { get; set; } = null!;
    public string? ResolveConfirmedValue(string? originalExtractedValue)
    {
        return Decision switch
        {
            FieldReviewDecision.Approved => originalExtractedValue,

            FieldReviewDecision.Corrected => CorrectedValue,

            FieldReviewDecision.Rejected => null,

            _ => null
        };
    }
}
