namespace Hakeem.Application.Configurations;

public sealed class MedicalCvPreviewLinkConfiguration
{
    public const string SectionName = "MedicalCvPreviewLink";

    public int LifetimeMinutes { get; set; } = 15;
}
