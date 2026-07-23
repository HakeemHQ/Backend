using System.Net;
using System.Net.Mail;
using Hakeem.Application.Configurations;
using Hakeem.Application.Interfaces;
using Hakeem.Application.Interfaces.Notifications;
using Microsoft.Extensions.Options;

namespace Hakeem.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettingsConfiguration _emailSettings;

    public EmailService(IOptions<EmailSettingsConfiguration> emailSettings)
    {
        _emailSettings = emailSettings.Value;
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = false)
    {
        var fromAddress = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName);
        var toAddress = new MailAddress(to);

        using var smtp = new SmtpClient
        {
            Host = _emailSettings.Host,
            Port = _emailSettings.Port,
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password)
        };

        using var message = new MailMessage(fromAddress, toAddress)
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = isHtml
        };

        await smtp.SendMailAsync(message);
    }
}
