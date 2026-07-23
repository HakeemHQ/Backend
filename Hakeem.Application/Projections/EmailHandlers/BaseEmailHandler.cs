using Hakeem.Application.Interfaces;
using Hakeem.Application.Interfaces.Notifications;
using Hakeem.Application.Repositories.Notifications;
using Hakeem.Domain.Enums.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Hakeem.Application.Projections.EmailHandlers;

/// <summary>
/// Shared routing logic for all outbox email handlers.
/// - When <see cref="EmailTemplate.RecipientKey"/> is set, addresses are resolved from <see cref="IEmailRecipientRepository"/>.
/// - When null, the <paramref name="dynamicRecipient"/> from the event payload is used.
/// </summary>
public abstract class BaseEmailHandler(
    IEmailService emailService,
    IEmailTemplateRepository templateRepository,
    IEmailRecipientRepository recipientRepository,
    ITemplateRenderer renderer,
    IConfiguration configuration,
    ILogger logger)
{
    /// <summary>
    /// Builds a full portal URL by combining the configured base URL with the given relative path.
    /// </summary>
    protected string BuildPortalLink(string relativePath)
    {
        var baseUrl = configuration["WebsiteSettings:BaseUrl"]?.TrimEnd('/') ?? string.Empty;
        return $"{baseUrl}/{relativePath.TrimStart('/')}";
    }
    /// <summary>
    /// Fetches the template from the database, renders placeholders, resolves recipient(s), and sends the email.
    /// </summary>
    protected async Task SendAsync(
        EmailTemplateKey key,
        Dictionary<string, string> placeholders,
        string? dynamicRecipient,
        CancellationToken cancellationToken)
    {
        var template = await templateRepository.GetByKeyAsync(key, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Email template '{key}' was not found or is inactive. Ensure the EmailTemplates table is seeded.");

        var subject = renderer.Render(template.Subject, placeholders);
        var body = renderer.Render(template.Body, placeholders);

        IEnumerable<string> recipients = template.RecipientKey.HasValue
            ? await recipientRepository.GetActiveEmailsAsync(template.RecipientKey.Value, cancellationToken)
            : [dynamicRecipient
                ?? throw new InvalidOperationException(
                    $"Template '{key}' has no RecipientKey configured and no dynamic recipient address was provided.")];

        foreach (var address in recipients)
        {
            logger.LogInformation("Sending email '{Key}' to {Address}.", key, address);
            await emailService.SendEmailAsync(address, subject, body, isHtml: true);
            logger.LogInformation("Email '{Key}' sent to {Address}.", key, address);
        }
    }
}
