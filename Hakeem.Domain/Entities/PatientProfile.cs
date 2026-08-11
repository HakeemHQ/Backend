using System;
using System.Collections.Generic;

namespace Hakeem.Domain.Entities;

using Hakeem.Domain.Enums.Identity;

public class PatientProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public string PatientCode { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    public IdentityVerificationStatus IdentityVerificationStatus { get; set; }
        = IdentityVerificationStatus.Pending;
    public string? VerifiedNationalId { get; set; }
    public Guid? VerifiedByDoctorId { get; set; }
    public DateTime? VerifiedAt { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual DoctorProfile? VerifiedByDoctor { get; set; }
    public virtual ICollection<MedicalDocument> MedicalDocuments { get; set; } = new List<MedicalDocument>();
    public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
    public virtual ICollection<MedicalCv> MedicalCvs { get; set; } = new List<MedicalCv>();
    public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
    public virtual ICollection<ConsentRecord> ConsentRecords { get; set; } = new List<ConsentRecord>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public virtual ICollection<PatientAccessRequest> DoctorAccessRequests { get; set; }
        = new List<PatientAccessRequest>();
    public virtual ICollection<DoctorPatientAccess> DoctorAccesses { get; set; }
        = new List<DoctorPatientAccess>();
}
