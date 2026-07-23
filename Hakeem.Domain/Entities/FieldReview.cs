using System;

namespace Hakeem.Domain.Entities;

public class FieldReview : BaseEntity
{
    public Guid ExtractedFieldId { get; set; }
    public Guid MedicalRecordId { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string CorrectedValue { get; set; } = string.Empty;
    public DateTime ReviewedAt { get; set; }

    public virtual ExtractedField ExtractedField { get; set; } = null!;
    public virtual MedicalRecord MedicalRecord { get; set; } = null!;
}
