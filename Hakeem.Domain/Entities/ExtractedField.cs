using System;
using System.Collections.Generic;

namespace Hakeem.Domain.Entities;

public class ExtractedField : BaseEntity
{
    public Guid DocumentId { get; set; }
    public string FieldGroup { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string ExtractedValue { get; set; } = string.Empty;
    public decimal Confidence { get; set; }

    public virtual MedicalDocument MedicalDocument { get; set; } = null!;
    public virtual FieldReview? FieldReview { get; set; }
}
