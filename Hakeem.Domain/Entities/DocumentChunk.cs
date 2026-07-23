using System;

namespace Hakeem.Domain.Entities;

public class DocumentChunk : BaseEntity
{
    public Guid DocumentId { get; set; }
    public int SequenceNumber { get; set; }
    public string PageReference { get; set; } = string.Empty;

    public virtual MedicalDocument MedicalDocument { get; set; } = null!;
}
