using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Interfaces.Notifications;

public interface IEmailService : ITransient
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = false);
}
