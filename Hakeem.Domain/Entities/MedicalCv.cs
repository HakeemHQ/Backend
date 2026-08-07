using System;
using System.Collections.Generic;

using Hakeem.Domain.Enums.MedicalCvs;

namespace Hakeem.Domain.Entities;

public class MedicalCv : BaseEntity
{
    public Guid PatientId { get; set; }
    public string Title { get; set; } = string.Empty;
    public MedicalCvScopeType ScopeType { get; set; }
    public string? Focus { get; set; }

    public virtual PatientProfile PatientProfile { get; set; } = null!;
    public virtual ICollection<MedicalCvVersion> Versions { get; set; } = new List<MedicalCvVersion>();
}
