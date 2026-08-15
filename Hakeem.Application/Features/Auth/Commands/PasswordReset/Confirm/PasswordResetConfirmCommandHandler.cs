using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Repositories.Auth;
using Hakeem.Application.Repositories.Users;
using Hakeem.Application.Interfaces.Services.Auth;
using Hakeem.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Hakeem.Application.Features.Auth.Commands.PasswordReset.Confirm;

public sealed class PasswordResetConfirmCommandHandler(
    IUserRepository userRepository,
    IPasswordResetTokenRepository passwordResetTokenRepository,
    ITokenService tokenService,
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

        var tokenHash = tokenService.HashPasswordResetToken(request.ResetToken);
        var storedToken = await passwordResetTokenRepository.GetByHashAsync(tokenHash, cancellationToken);

        if (storedToken is null ||
            storedToken.IsUsed ||
            storedToken.ExpiresAt < DateTime.UtcNow)
        {
            throw new LocalizedHttpException(ErrorCodes.AuthInvalidResetToken, StatusCodes.Status422UnprocessableEntity);
        }

        var user = await userRepository.GetByIdAsync(storedToken.UserId, cancellationToken);
        if (user is null)
        {
            throw new LocalizedHttpException(ErrorCodes.AuthInvalidResetToken, StatusCodes.Status422UnprocessableEntity);
        }

        if (passwordHasher.Verify(request.NewPassword, user.PasswordHash))
        {
            throw new LocalizedHttpException(
                ErrorCodes.AuthPasswordMustBeDifferent,
                StatusCodes.Status422UnprocessableEntity);
        }

        storedToken.IsUsed = true;
        user.PasswordHash = passwordHasher.Hash(request.NewPassword);

        await unitOfWork.SaveChanges(cancellationToken);

        return Unit.Value;
    }
}
