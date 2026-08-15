using System;
using System.Collections.Generic;
using Hakeem.Domain.Enums.MedicalCvs;

namespace Hakeem.Domain.Entities;

public class MedicalCvVersion : BaseEntity
{
    public Guid MedicalCvId { get; set; }
    public int VersionNumber { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public MedicalCvCreatedByRole CreatedByRole { get; set; }
        = MedicalCvCreatedByRole.Unknown;
    public MedicalCvVersionStatus Status { get; set; } = MedicalCvVersionStatus.Queued;
    public string PdfFileKey { get; set; } = string.Empty;
    public DateTime? ApprovedAt { get; set; }

    public virtual MedicalCv MedicalCv { get; set; } = null!;
    public virtual User? CreatedByUser { get; set; }
    public virtual ICollection<MedicalRecord> SummarizedRecords { get; set; } = new List<MedicalRecord>();
    public virtual ICollection<SharedCvLink> SharedCvLinks { get; set; } = new List<SharedCvLink>();
}
