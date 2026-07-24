using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Common.Interfaces;

public interface ICurrentUserContext
{
    Guid UserId { get; }
}
