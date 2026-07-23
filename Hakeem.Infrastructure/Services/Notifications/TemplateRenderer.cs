using Hakeem.Application.Interfaces;
using Hakeem.Application.Interfaces.Notifications;

namespace Hakeem.Infrastructure.Services;

/// <summary>
/// Replaces {{Placeholder}} tokens in a template string with values from the provided dictionary.
/// Keys are matched case-sensitively. Missing keys leave the token unchanged.
/// </summary>
public class TemplateRenderer : ITemplateRenderer
{
    public string Render(string template, Dictionary<string, string> placeholders)
    {
        foreach (var (key, value) in placeholders)
            template = template.Replace($"{{{{{key}}}}}", value ?? string.Empty,
                StringComparison.Ordinal);

        return template;
    }
}
