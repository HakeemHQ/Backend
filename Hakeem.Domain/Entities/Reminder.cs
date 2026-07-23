using System;

namespace Hakeem.Domain.Entities;

public class Reminder : BaseEntity
{
    public Guid PatientProfileId { get; set; }
    public Guid? MedicalRecordId { get; set; }
    public string ReminderType { get; set; } = string.Empty;
    public DateTime ScheduledFor { get; set; }
    public string RepeatPattern { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public virtual PatientProfile PatientProfile { get; set; } = null!;
    public virtual MedicalRecord? MedicalRecord { get; set; }
}
