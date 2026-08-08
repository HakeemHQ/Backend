namespace Hakeem.Application.Configurations;

public sealed class MedicalIntelligenceAgentConfiguration
{
    public const string SectionName = "MedicalIntelligenceAgent";

    public int SearchLimit { get; set; } = 15;

    public float FocusedCvMinimumScore { get; set; } = 0.5f;

    public float FocusedCvRelatedEvidenceMinimumScore { get; set; } = 0.35f;
}
