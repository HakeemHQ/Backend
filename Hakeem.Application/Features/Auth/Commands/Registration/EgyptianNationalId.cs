namespace Hakeem.Application.Features.Auth.Commands.Registration;

public static class EgyptianNationalId
{
    private static readonly HashSet<string> ValidGovernorateCodes =
    [
        "01", "02", "03", "04",
        "11", "12", "13", "14", "15", "16", "17", "18", "19",
        "21", "22", "23", "24", "25", "26", "27", "28", "29",
        "31", "32", "33", "34", "35", "88"
    ];

    public static bool IsStructurallyValid(string? nationalId, DateOnly birthDate)
    {
        if (nationalId is null || nationalId.Length != 14 ||
            nationalId.Any(character => !char.IsAsciiDigit(character)))
        {
            return false;
        }

        var century = nationalId[0] switch
        {
            '2' => 1900,
            '3' => 2000,
            _ => 0
        };

        if (century == 0 || !ValidGovernorateCodes.Contains(nationalId.Substring(7, 2)))
        {
            return false;
        }

        if (!int.TryParse(nationalId.AsSpan(1, 2), out var year) ||
            !int.TryParse(nationalId.AsSpan(3, 2), out var month) ||
            !int.TryParse(nationalId.AsSpan(5, 2), out var day))
        {
            return false;
        }

        try
        {
            return new DateOnly(century + year, month, day) == birthDate;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }
}
