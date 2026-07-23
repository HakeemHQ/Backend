using System;
using System.Collections.Generic;

namespace Hakeem.Domain.Entities;

public class ConsentRecord : BaseEntity
{
    public Guid PatientProfileId { get; set; }
    public string ConsentType { get; set; } = string.Empty;
    public string Decision { get; set; } = string.Empty;
    public DateTime EffectiveAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public virtual PatientProfile PatientProfile { get; set; } = null!;
    public virtual ICollection<SharedCvLink> SharedCvLinks { get; set; } = new List<SharedCvLink>();
}
