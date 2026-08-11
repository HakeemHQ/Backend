using Hakeem.Domain.Enums.Access;

namespace Hakeem.Domain.Entities;

public class DoctorPatientAccess : BaseEntity
{
    public Guid PatientAccessRequestId { get; set; }
    public Guid DoctorProfileId { get; set; }
    public Guid PatientProfileId { get; set; }
    public DoctorPatientAccessStatus Status { get; set; }
        = DoctorPatientAccessStatus.Active;
    public DateTime GrantedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public virtual PatientAccessRequest PatientAccessRequest { get; set; } = null!;
    public virtual DoctorProfile Doctor { get; set; } = null!;
    public virtual PatientProfile Patient { get; set; } = null!;
}
