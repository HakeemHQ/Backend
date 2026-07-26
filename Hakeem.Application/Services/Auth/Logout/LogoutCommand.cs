using MediatR;

namespace Hakeem.Application.Services.Auth.Logout;

public sealed record LogoutCommand(Guid UserId, string JwtId) : IRequest<Unit>;
