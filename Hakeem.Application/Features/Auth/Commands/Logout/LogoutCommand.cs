using MediatR;

namespace Hakeem.Application.Features.Auth.Commands.Logout;

public sealed record LogoutCommand(Guid UserId, string JwtId) : IRequest<Unit>;
