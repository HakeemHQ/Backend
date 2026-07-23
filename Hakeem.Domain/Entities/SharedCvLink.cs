using System;

namespace Hakeem.Domain.Entities;

public class SharedCvLink : BaseEntity
{
    public Guid MedicalCvVersionId { get; set; }
    public Guid ConsentRecordId { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string Status { get; set; } = string.Empty;

    public virtual MedicalCvVersion MedicalCvVersion { get; set; } = null!;
    public virtual ConsentRecord ConsentRecord { get; set; } = null!;
}
