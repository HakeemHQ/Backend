using Hakeem.Application.Repositories.Auth;
using Hakeem.Domain.Interfaces;
using MediatR;

namespace Hakeem.Application.Services.Auth.Logout;

public sealed class LogoutCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<LogoutCommand, Unit>
{
    public async Task<Unit> Handle(
        LogoutCommand request,
        CancellationToken cancellationToken)
    {
        var session = await refreshTokenRepository.GetByJwtIdAsync(
            request.JwtId,
            request.UserId,
            cancellationToken);

        if (session is not null && !session.IsRevoked)
        {
            session.IsRevoked = true;
            await unitOfWork.SaveChanges(cancellationToken);
        }

        return Unit.Value;
    }
}
