using Hakeem.Infrastructure.AI.Agents.Prompts;

namespace Hakeem.Infrastructure.Tests.AI.Agents;

public sealed class MedicalDocumentClassificationPromptTests
{
    [Fact]
    public void SystemPrompt_DefinesMedicalAndNonMedicalBoundaries()
    {
        Assert.Contains("prescriptions", MedicalDocumentClassificationPrompt.System);
        Assert.Contains("laboratory or imaging reports", MedicalDocumentClassificationPrompt.System);
        Assert.Contains("appointment or follow-up", MedicalDocumentClassificationPrompt.System);
        Assert.Contains("grocery lists", MedicalDocumentClassificationPrompt.System);
        Assert.Contains("random photographs", MedicalDocumentClassificationPrompt.System);
        Assert.Contains("blank files", MedicalDocumentClassificationPrompt.System);
    }

    [Fact]
    public void SystemPrompt_RejectsSingleKeywordClassificationAndClinicalJudgment()
    {
        Assert.Contains(
            "generic word such as doctor, health, or medicine",
            MedicalDocumentClassificationPrompt.System);
        Assert.Contains(
            "Do not diagnose",
            MedicalDocumentClassificationPrompt.System);
        Assert.Contains(
            "is still medical",
            MedicalDocumentClassificationPrompt.System);
    }

    [Fact]
    public void SystemPrompt_RequiresStructuredClassificationTool()
    {
        Assert.Contains("submit_classification", MedicalDocumentClassificationPrompt.System);
        Assert.Contains("rejectionReason is null", MedicalDocumentClassificationPrompt.System);
        Assert.Contains("documentType is null", MedicalDocumentClassificationPrompt.System);
    }
}
