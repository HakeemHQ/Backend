using System;

namespace Hakeem.Domain.Entities;

public class SourceReference : BaseEntity
{
    public Guid MedicalRecordId { get; set; }
    public Guid DocumentId { get; set; }
    public string PageReference { get; set; } = string.Empty;

    public virtual MedicalRecord MedicalRecord { get; set; } = null!;
    public virtual MedicalDocument MedicalDocument { get; set; } = null!;
}
