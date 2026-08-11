using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Repositories.Auth;
using Hakeem.Application.Interfaces.Services.Auth;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces;
using Hakeem.Domain.Enums.Identity;
using MediatR;

namespace Hakeem.Application.Features.Auth.Commands.Refresh;

public sealed class RefreshCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    ITokenService tokenService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RefreshCommand, RefreshResult>
{
    public async Task<RefreshResult> Handle(
        RefreshCommand request,
        CancellationToken cancellationToken)
    {
        var hashedToken = tokenService.HashRefreshToken(request.RefreshToken);
        var storedToken = await refreshTokenRepository.GetByTokenAsync(
            hashedToken,
            cancellationToken);

        if (storedToken is null ||
            storedToken.IsUsed ||
            storedToken.IsRevoked ||
            storedToken.ExpiryDate <= DateTime.UtcNow ||
            storedToken.User.Status != AccountStatus.Active)
        {
            throw new UnAuthorizedException(ErrorCodes.AuthInvalidRefreshToken);
        }

        storedToken.IsUsed = true;

        var issuedTokens = tokenService.CreateTokenPair(storedToken.User);

        refreshTokenRepository.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = storedToken.UserId,
            Token = tokenService.HashRefreshToken(issuedTokens.RefreshToken),
            JwtId = issuedTokens.JwtId,
            IsUsed = false,
            IsRevoked = false,
            AddedDate = DateTime.UtcNow,
            ExpiryDate = issuedTokens.RefreshTokenExpiresAt
        });

        await unitOfWork.SaveChanges(cancellationToken);

        return new RefreshResult(
            issuedTokens.AccessToken,
            issuedTokens.RefreshToken,
            "Bearer",
            issuedTokens.AccessTokenExpiresAt,
            issuedTokens.RefreshTokenExpiresAt);
    }
}
