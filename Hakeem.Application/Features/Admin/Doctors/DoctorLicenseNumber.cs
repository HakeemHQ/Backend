namespace Hakeem.Application.Features.Admin.Doctors;

public static class DoctorLicenseNumber
{
    private const string Prefix = "HKM-DR-";

    public static string Generate(Guid doctorId)
    {
        if (doctorId == Guid.Empty)
        {
            throw new ArgumentException(
                "A doctor ID is required to generate a license number.",
                nameof(doctorId));
        }

        return $"{Prefix}{doctorId:N}".ToUpperInvariant();
    }
}

