using Hakeem.Domain.Entities.NotificationsEntites;
using Hakeem.Domain.Enums.Notifications;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Repositories.Notifications;

public interface IEmailTemplateRepository : IScoped
{
    /// <summary>
    /// Returns the active template for the given key, or null if not found / inactive.
    /// </summary>
    Task<EmailTemplate?> GetByKeyAsync(EmailTemplateKey key, CancellationToken cancellationToken = default);
}
