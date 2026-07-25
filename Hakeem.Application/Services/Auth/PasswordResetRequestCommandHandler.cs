using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Repositories.Notifications;
using Hakeem.Application.Repositories.Users;
using Hakeem.Domain.DomainEvents.Outbox;
using Hakeem.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Hakeem.Application.Services.Auth;

public sealed class PasswordResetRequestCommandHandler(
    IUserRepository userRepository,
    IPasswordResetTokenStore passwordResetTokenStore,
    IOutboxEventRepository outboxEventRepository,
    IUnitOfWork unitOfWork,
    IConfiguration configuration)
    : IRequestHandler<PasswordResetRequestCommand, Unit>
{
    public async Task<Unit> Handle(PasswordResetRequestCommand request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Email))
        {
            throw new LocalizedHttpException(ErrorCodes.ValidationInvalid, StatusCodes.Status400BadRequest);
        }

        var normalizedEmail = request.Email.Trim();
        var user = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user is not null)
        {
            var resetToken = await passwordResetTokenStore.CreateTokenAsync(user.Id, cancellationToken);
            var resetLink = BuildResetLink(resetToken);
            var passwordResetEvent = new PasswordResetEmailEvent
            {
                RecipientEmail = user.Email,
                UserName = string.IsNullOrWhiteSpace(user.FirstName) ? user.Email : user.FirstName,
                ResetLink = resetLink
            };

            outboxEventRepository.Add(passwordResetEvent);
            await unitOfWork.SaveChanges(cancellationToken);
        }

        return Unit.Value;
    }

    private string BuildResetLink(string resetToken)
    {
        var baseUrl = configuration["WebsiteSettings:BaseUrl"]?.TrimEnd('/') ?? string.Empty;
        return $"{baseUrl}/auth/password-reset/confirm?token={Uri.EscapeDataString(resetToken)}";
    }
}
