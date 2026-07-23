using System;
using System.Collections.Generic;

namespace Hakeem.Domain.Entities;

public class MedicalRecord : BaseEntity
{
    public Guid PatientProfileId { get; set; }
    public string RecordType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime ClinicalDate { get; set; }
    public string Status { get; set; } = string.Empty;

    public virtual PatientProfile PatientProfile { get; set; } = null!;
    public virtual ICollection<FieldReview> FieldReviews { get; set; } = new List<FieldReview>();
    public virtual ICollection<SourceReference> SourceReferences { get; set; } = new List<SourceReference>();
    public virtual ICollection<MedicalCvVersion> SummarizedInCvVersions { get; set; } = new List<MedicalCvVersion>();
    public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
}
