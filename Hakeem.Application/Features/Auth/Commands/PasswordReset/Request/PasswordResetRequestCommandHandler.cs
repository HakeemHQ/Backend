using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Repositories.Notifications;
using Hakeem.Application.Repositories.Users;
using Hakeem.Application.Repositories.Auth;
using Hakeem.Application.Interfaces.Services.Auth;
using Hakeem.Domain.Entities;
using Hakeem.Domain.DomainEvents.Outbox;
using Hakeem.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Hakeem.Application.Features.Auth.Commands.PasswordReset.Request;

public sealed class PasswordResetRequestCommandHandler(
    IUserRepository userRepository,
    IPasswordResetTokenRepository passwordResetTokenRepository,
    ITokenService tokenService,
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
            var issuedToken = tokenService.CreatePasswordResetToken();

            passwordResetTokenRepository.Add(new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = issuedToken.TokenHash,
                ExpiresAt = issuedToken.ExpiresAt,
                IsUsed = false
            });

            var resetLink = BuildResetLink(issuedToken.Token);
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
        var frontendUrl = configuration["PasswordReset:FrontendUrl"];
        if (string.IsNullOrWhiteSpace(frontendUrl))
        {
            throw new InvalidOperationException("PasswordReset:FrontendUrl is not configured.");
        }

        return $"{frontendUrl.Trim()}?token={Uri.EscapeDataString(resetToken)}";
    }
}
