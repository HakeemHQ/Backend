namespace Hakeem.Application.Configurations;

public sealed class PatientAccessConfiguration
{
    public const string SectionName = "PatientAccess";

    public int CodeLifetimeMinutes { get; set; } = 6;
    public int AccessLifetimeMinutes { get; set; } = 120;
}
