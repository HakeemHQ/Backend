namespace Hakeem.Domain.Entities;

public class MedicalRecord : BaseEntity
{
    public Guid PatientProfileId { get; set; }
    public Guid? SourceExtractedItemId { get; set; }
    public string RecordType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime ClinicalDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public virtual PatientProfile PatientProfile { get; set; } = null!;
    public virtual ExtractedItem? SourceExtractedItem { get; set; }
    public virtual ICollection<MedicalRecordField> Fields { get; set; } = new List<MedicalRecordField>();
    public virtual ICollection<SourceReference> SourceReferences { get; set; } = new List<SourceReference>();
    public virtual ICollection<MedicalCvVersion> SummarizedInCvVersions { get; set; } = new List<MedicalCvVersion>();
    public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
}
