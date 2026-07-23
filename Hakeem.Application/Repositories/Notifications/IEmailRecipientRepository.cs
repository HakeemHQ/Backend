using Hakeem.Domain.Enums.Notifications;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Repositories.Notifications;

public interface IEmailRecipientRepository : IScoped
{
    /// <summary>
    /// Returns all active email addresses registered for the given department key.
    /// Multiple addresses may be returned; each receives its own email.
    /// </summary>
    Task<IReadOnlyList<string>> GetActiveEmailsAsync(EmailRecipientKey key, CancellationToken cancellationToken = default);
}
