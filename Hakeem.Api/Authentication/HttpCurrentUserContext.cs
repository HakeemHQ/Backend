using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Hakeem.Application.Common.Interfaces;

namespace Hakeem.Api.Authentication;

public sealed class HttpCurrentUserContext(IHttpContextAccessor httpContextAccessor)
    : ICurrentUserContext
{
    public Guid UserId
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;

            if (principal?.Identity?.IsAuthenticated != true)
            {
                throw new UnauthorizedAccessException("An authenticated user context is required.");
            }

            var userIdValue =
                principal.FindFirstValue(ClaimTypes.NameIdentifier) ??
                principal.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                principal.FindFirstValue("userId");

            if (!Guid.TryParse(userIdValue, out var userId))
            {
                throw new UnauthorizedAccessException("The user identifier claim is missing or invalid.");
            }

            return userId;
        }
    }
}
