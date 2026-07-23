using Hakeem.Application.Repositories.Notifications;
using Hakeem.Domain.Enums;
using Hakeem.Domain.Enums.Notifications;
using Hakeem.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Hakeem.Infrastructure.Repositories.Notifications;

public class EmailRecipientRepository(ApplicationDbContext dbContext) : IEmailRecipientRepository
{
    public async Task<IReadOnlyList<string>> GetActiveEmailsAsync(
        EmailRecipientKey key,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.EmailRecipients
            .AsNoTracking()
            .Where(r => r.RecipientKey == key && r.IsActive)
            .Select(r => r.EmailAddress)
            .ToListAsync(cancellationToken);
    }
}
