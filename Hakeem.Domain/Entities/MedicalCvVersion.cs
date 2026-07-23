using System;
using System.Collections.Generic;

namespace Hakeem.Domain.Entities;

public class MedicalCvVersion : BaseEntity
{
    public Guid MedicalCvId { get; set; }
    public int VersionNumber { get; set; }
    public string Status { get; set; } = string.Empty;

    public virtual MedicalCv MedicalCv { get; set; } = null!;
    public virtual ICollection<MedicalRecord> SummarizedRecords { get; set; } = new List<MedicalRecord>();
    public virtual ICollection<SharedCvLink> SharedCvLinks { get; set; } = new List<SharedCvLink>();
}
