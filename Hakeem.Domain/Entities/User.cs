using System;
using System.Collections.Generic;

namespace Hakeem.Domain.Entities;

using Hakeem.Domain.Enums.Identity;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public ApplicationRole Role { get; set; } = ApplicationRole.Patient;
    public AccountStatus Status { get; set; } = AccountStatus.Active;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public virtual PatientProfile? PatientProfile { get; set; }
    public virtual DoctorProfile? DoctorProfile { get; set; }
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
}
