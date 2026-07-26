using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Repositories.Auth;
using Hakeem.Application.Repositories.Users;
using Hakeem.Application.Interfaces.Services.Auth;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces;
using MediatR;

namespace Hakeem.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<LoginCommand, LoginResult>
{
    private const string ActiveStatus = "Active";

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnAuthorizedException(ErrorCodes.AuthInvalidCredentials);
        }

        if (!string.Equals(user.Status, ActiveStatus, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnAuthorizedException(ErrorCodes.AuthAccountInactive);
        }

        var issuedTokens = tokenService.CreateTokenPair(user);

        refreshTokenRepository.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = tokenService.HashRefreshToken(issuedTokens.RefreshToken),
            JwtId = issuedTokens.JwtId,
            IsUsed = false,
            IsRevoked = false,
            AddedDate = DateTime.UtcNow,
            ExpiryDate = issuedTokens.RefreshTokenExpiresAt
        });

        await unitOfWork.SaveChanges(cancellationToken);

        return new LoginResult(
            issuedTokens.AccessToken,
            issuedTokens.RefreshToken,
            "Bearer",
            issuedTokens.AccessTokenExpiresAt,
            issuedTokens.RefreshTokenExpiresAt,
            new LoginUserResult(user.Id, user.Email, user.UserType));
    }
}
