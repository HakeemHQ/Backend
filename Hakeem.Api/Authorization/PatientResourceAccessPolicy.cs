using Microsoft.AspNetCore.Authorization;

namespace Hakeem.Api.Authorization;

public static class PatientResourceAccessPolicy
{
    public const string DoctorOrVerifiedPatient = "DoctorOrVerifiedPatientResourceAccess";
    public const string DoctorOnly = "DoctorPatientResourceAccess";
}

public sealed record PatientResourceAccessRequirement(
    bool AllowVerifiedPatient) : IAuthorizationRequirement;
