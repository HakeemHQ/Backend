using Hakeem.Application.Abstractions;
using Hakeem.Application.Interfaces.Notifications;
using Hakeem.Application.Repositories.Notifications;
using Hakeem.Domain.DomainEvents.Outbox;

using Hakeem.Domain.Enums.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Hakeem.Application.Projections.EmailHandlers;

public class WelcomeEmailHandler(
    IEmailService emailService,
    IEmailTemplateRepository templateRepository,
    IEmailRecipientRepository recipientRepository,
    ITemplateRenderer renderer,
    IConfiguration configuration,
    ILogger<WelcomeEmailHandler> logger)
    : BaseEmailHandler(emailService, templateRepository, recipientRepository, renderer, configuration, logger),
      IOutboxEventHandler<WelcomeEmailEvent>
{
    public async Task HandleAsync(WelcomeEmailEvent @event, CancellationToken cancellationToken)
    {
        var placeholders = new Dictionary<string, string>
        {
            ["UserName"] = @event.UserNameEn ?? string.Empty,
            ["TempPassword"] = @event.Password
        };

        await SendAsync(EmailTemplateKey.WelcomeEmail, placeholders, @event.RecipientEmail, cancellationToken);
    }
}
