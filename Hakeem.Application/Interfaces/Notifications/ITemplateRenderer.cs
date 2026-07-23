using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Interfaces.Notifications;

/// <summary>
/// Substitutes {{Placeholder}} tokens in a template string with values from the provided dictionary.
/// </summary>
public interface ITemplateRenderer : ITransient
{
    string Render(string template, Dictionary<string, string> placeholders);
}
