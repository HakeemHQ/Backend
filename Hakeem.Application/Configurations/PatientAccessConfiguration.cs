namespace Hakeem.Application.Configurations;

public sealed class PatientAccessConfiguration
{
    public const string SectionName = "PatientAccess";

    public int PendingRequestLifetimeMinutes { get; set; } = 30;
    public int CodeLifetimeMinutes { get; set; } = 6;
    public int AccessLifetimeMinutes { get; set; } = 120;
}
