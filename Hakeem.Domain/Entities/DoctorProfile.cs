namespace Hakeem.Domain.Entities;

public class DoctorProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public string Specialty { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;

    public virtual User User { get; set; } = null!;
    public virtual ICollection<PatientProfile> VerifiedPatients { get; set; } = new List<PatientProfile>();
    public virtual ICollection<PatientAccessRequest> PatientAccessRequests { get; set; }
        = new List<PatientAccessRequest>();
    public virtual ICollection<DoctorPatientAccess> PatientAccesses { get; set; }
        = new List<DoctorPatientAccess>();
}
