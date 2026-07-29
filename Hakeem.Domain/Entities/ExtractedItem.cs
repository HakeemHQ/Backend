using System;
using System.Collections.Generic;

namespace Hakeem.Domain.Entities;

public class ExtractedItem : BaseEntity
{
    public Guid MedicalDocumentId { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public int SequenceNumber { get; set; }
    public int PageNumber { get; set; }

    public virtual MedicalDocument MedicalDocument { get; set; } = null!;
    public virtual ICollection<ExtractedField> ExtractedFields { get; set; } = new List<ExtractedField>();
}
