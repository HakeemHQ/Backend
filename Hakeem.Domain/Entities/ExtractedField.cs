using System;
using System.Collections.Generic;

namespace Hakeem.Domain.Entities;

public class ExtractedField : BaseEntity
{
    public Guid ExtractedItemId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string ExtractedValue { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public string EvidenceText { get; set; } = string.Empty;
    public string Issues { get; set; } = string.Empty;

    public virtual ExtractedItem ExtractedItem { get; set; } = null!;
    public virtual FieldReview? FieldReview { get; set; }
}
