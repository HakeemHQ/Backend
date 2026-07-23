using Hakeem.Application.Interfaces;
using Hakeem.Application.Interfaces.Notifications;

namespace Hakeem.Infrastructure.Services;


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
