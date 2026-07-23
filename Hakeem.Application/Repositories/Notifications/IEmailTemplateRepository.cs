using Hakeem.Domain.Entities.NotificationsEntites;
using Hakeem.Domain.Enums.Notifications;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Repositories.Notifications;

public interface IEmailTemplateRepository : IScoped
{

    Task<EmailTemplate?> GetByKeyAsync(EmailTemplateKey key, CancellationToken cancellationToken = default);
}
