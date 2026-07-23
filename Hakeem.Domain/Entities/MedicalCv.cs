using System;
using System.Collections.Generic;

namespace Hakeem.Domain.Entities;

public class MedicalCv : BaseEntity
{
    public Guid PatientProfileId { get; set; }
    public string Title { get; set; } = string.Empty;

    public virtual PatientProfile PatientProfile { get; set; } = null!;
    public virtual ICollection<MedicalCvVersion> Versions { get; set; } = new List<MedicalCvVersion>();
}
