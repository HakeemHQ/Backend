using Hakeem.Application.Common.Interfaces;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Api.Authentication;

public sealed class DevelopmentCurrentUserContext : ICurrentUserContext
{
    public DevelopmentCurrentUserContext(IConfiguration configuration)
    {
        var configuredUserId = configuration["DevelopmentUser:UserId"];

        if (!Guid.TryParse(configuredUserId, out var userId))
        {
            throw new InvalidOperationException(
                "DevelopmentUser:UserId is missing or invalid.");
        }

        UserId = userId;
    }

    public Guid UserId { get; }
}
