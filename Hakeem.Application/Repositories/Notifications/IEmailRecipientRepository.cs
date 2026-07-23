using Hakeem.Domain.Enums.Notifications;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Repositories.Notifications;

public interface IEmailRecipientRepository : IScoped
{

    Task<IReadOnlyList<string>> GetActiveEmailsAsync(EmailRecipientKey key, CancellationToken cancellationToken = default);
}
