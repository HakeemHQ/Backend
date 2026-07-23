using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Interfaces.Notifications;

public interface ITemplateRenderer : ITransient
{
    string Render(string template, Dictionary<string, string> placeholders);
}
