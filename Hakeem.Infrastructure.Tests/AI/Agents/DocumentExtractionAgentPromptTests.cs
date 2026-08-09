using Hakeem.Infrastructure.AI.Agents.Prompts;

namespace Hakeem.Infrastructure.Tests.AI.Agents;

public sealed class DocumentExtractionAgentPromptTests
{
    [Fact]
    public void SystemPrompt_UsesCanonicalLabReportClassification()
    {
        Assert.Contains(
            "A blood-test report is LabReport.",
            DocumentExtractionAgentPrompt.System);
        Assert.DoesNotContain(
            "Blood Report",
            DocumentExtractionAgentPrompt.System);
    }

    [Fact]
    public void SystemPrompt_ProvidesSeparateItemsForTwoLabRows()
    {
        const string labResultItem = "\"itemType\": \"LabResult\"";

        Assert.Equal(
            2,
            CountOccurrences(
                DocumentExtractionAgentPrompt.System,
                labResultItem));
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var startIndex = 0;

        while ((startIndex = text.IndexOf(
                   value,
                   startIndex,
                   StringComparison.Ordinal)) >= 0)
        {
            count++;
            startIndex += value.Length;
        }

        return count;
    }
}
