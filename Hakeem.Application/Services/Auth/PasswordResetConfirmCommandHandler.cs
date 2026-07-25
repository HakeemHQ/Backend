using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Repositories.Users;
using Hakeem.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Hakeem.Application.Services.Auth;

public sealed class PasswordResetConfirmCommandHandler(
    IUserRepository userRepository,
    IPasswordResetTokenStore passwordResetTokenStore,
    IUnitOfWork unitOfWork)
    : IRequestHandler<PasswordResetConfirmCommand, Unit>
{
    public async Task<Unit> Handle(PasswordResetConfirmCommand request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.ResetToken) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new LocalizedHttpException(ErrorCodes.ValidationInvalid, StatusCodes.Status400BadRequest);
        }

        var (isValid, userId) = await passwordResetTokenStore.ValidateAndConsumeAsync(request.ResetToken, cancellationToken);
        if (!isValid || userId == Guid.Empty)
        {
            throw new LocalizedHttpException(ErrorCodes.AuthInvalidResetToken, StatusCodes.Status422UnprocessableEntity);
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new LocalizedHttpException(ErrorCodes.AuthInvalidResetToken, StatusCodes.Status422UnprocessableEntity);
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordHash = passwordHash;

        await unitOfWork.SaveChanges(cancellationToken);

        return Unit.Value;
    }
}
