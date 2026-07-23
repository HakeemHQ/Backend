using System;

namespace Hakeem.Domain.Entities;

public class AuditLog : BaseEntity
{
    public Guid? ActorUserId { get; set; }
    public Guid? PatientProfileId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }

    public virtual User? ActorUser { get; set; }
    public virtual PatientProfile? PatientProfile { get; set; }
}
