namespace Hakeem.Application.Features.PatientProfile.DTOs;

public static class NationalIdMasker
{
    public static string Mask(string? nationalId)
    {
        if (string.IsNullOrWhiteSpace(nationalId))
        {
            return string.Empty;
        }

        const int visibleDigits = 4;
        if (nationalId.Length <= visibleDigits)
        {
            return new string('*', nationalId.Length);
        }

        return new string('*', nationalId.Length - visibleDigits) + nationalId[^visibleDigits..];
    }
}
