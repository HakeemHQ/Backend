using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Repositories.PushDevices;

public interface IPushDeviceRepository : IScoped
{
    Task<PushDevice?> GetByExpoPushTokenAsync(
        string expoPushToken,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PushDevice>> GetActiveForUserAsync(
        Guid userId,
        CancellationToken cancellationToken);

    void Add(PushDevice pushDevice);
}
