using Hakeem.Application.Abstractions;
using Hakeem.Application.Interfaces.Notifications;
using Hakeem.Application.Repositories.Notifications;
using Hakeem.Domain.DomainEvents.Outbox;
using Hakeem.Domain.Enums.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Hakeem.Application.Projections.EmailHandlers;

public class PasswordResetEmailHandler(
    IEmailService emailService,
    IEmailTemplateRepository templateRepository,
    IEmailRecipientRepository recipientRepository,
    ITemplateRenderer renderer,
    IConfiguration configuration,
    ILogger<PasswordResetEmailHandler> logger)
    : BaseEmailHandler(emailService, templateRepository, recipientRepository, renderer, configuration, logger),
      IOutboxEventHandler<PasswordResetEmailEvent>
{
    public async Task HandleAsync(PasswordResetEmailEvent @event, CancellationToken cancellationToken)
    {
        var placeholders = new Dictionary<string, string>
        {
            ["UserName"] = @event.UserName,
            ["ResetLink"] = @event.ResetLink
        };

        await SendAsync(EmailTemplateKey.PasswordReset, placeholders, @event.RecipientEmail, cancellationToken);
    }
}
