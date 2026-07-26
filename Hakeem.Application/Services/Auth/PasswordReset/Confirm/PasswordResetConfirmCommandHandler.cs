using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Repositories.Users;
using Hakeem.Application.Services.Auth.PasswordHashing;
using Hakeem.Application.Services.Auth.PasswordReset;
using Hakeem.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Hakeem.Application.Services.Auth.PasswordReset.Confirm;

public sealed class PasswordResetConfirmCommandHandler(
    IUserRepository userRepository,
    IPasswordResetTokenStore passwordResetTokenStore,
    IPasswordHasher passwordHasher,
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

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);

        await unitOfWork.SaveChanges(cancellationToken);

        return Unit.Value;
    }
}
