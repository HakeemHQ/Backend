using System.Globalization;
using Hakeem.Application.Features.MedicalCvs.DTOs;

namespace Hakeem.Application.Resources;

public static class MedicalCvResourceText
{
    public static string Get(string key, string? language)
    {
        var culture = CultureInfo.GetCultureInfo(
            MedicalCvLanguages.Normalize(language));

        return SharedResource.ResourceManager.GetString(key, culture)
            ?? throw new InvalidOperationException(
                $"Medical CV resource key '{key}' was not found.");
    }

    public static string Format(
        string key,
        string? language,
        params object?[] arguments)
    {
        var culture = CultureInfo.GetCultureInfo(
            MedicalCvLanguages.Normalize(language));
        return string.Format(culture, Get(key, language), arguments);
    }
}
