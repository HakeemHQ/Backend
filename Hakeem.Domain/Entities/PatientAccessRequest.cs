using Hakeem.Domain.Enums.Access;

namespace Hakeem.Domain.Entities;

public class PatientAccessRequest : BaseEntity
{
    public Guid DoctorProfileId { get; set; }
    public Guid PatientProfileId { get; set; }
    public PatientAccessRequestStatus Status { get; set; }
        = PatientAccessRequestStatus.Pending;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public string? CodeHash { get; set; }
    public DateTime? CodeExpiresAt { get; set; }
    public DateTime? RedeemedAt { get; set; }

    public virtual DoctorProfile Doctor { get; set; } = null!;
    public virtual PatientProfile Patient { get; set; } = null!;
    public virtual DoctorPatientAccess? Access { get; set; }
}
