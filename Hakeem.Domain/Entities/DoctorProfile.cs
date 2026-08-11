namespace Hakeem.Domain.Entities;

public class DoctorProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public string Specialty { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;

    public virtual User User { get; set; } = null!;
    public virtual ICollection<PatientProfile> VerifiedPatients { get; set; } = new List<PatientProfile>();
}
