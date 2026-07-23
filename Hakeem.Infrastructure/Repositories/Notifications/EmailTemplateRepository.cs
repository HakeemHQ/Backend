
using Hakeem.Application.Repositories.Notifications;
using Hakeem.Domain.Entities.NotificationsEntites;
using Hakeem.Domain.Enums;
using Hakeem.Domain.Enums.Notifications;
using Hakeem.Infrastructure.Context;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Hakeem.Infrastructure.Repositories.Notifications;

public class EmailTemplateRepository(ApplicationDbContext dbContext, IMemoryCache cache) : IEmailTemplateRepository
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public async Task<EmailTemplate?> GetByKeyAsync(EmailTemplateKey key, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"EmailTemplate:{(int)key}";

        if (cache.TryGetValue(cacheKey, out EmailTemplate? cached))
            return cached;

        var template = await dbContext.EmailTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TemplateKey == key && t.IsActive, cancellationToken);

        if (template is not null)
            cache.Set(cacheKey, template, CacheDuration);

        return template;
    }
}
