namespace Hakeem.Api.Authorization;

public static class DoctorPatientAccessPolicy
{
    public const string Name = "DoctorPatientAccess";
    public const string PatientIdRouteValue = "patientId";
    public const string PatientProfileIdRouteValue = "patientProfileId";
}

public sealed record DoctorPatientAccessResource(Guid PatientId);
