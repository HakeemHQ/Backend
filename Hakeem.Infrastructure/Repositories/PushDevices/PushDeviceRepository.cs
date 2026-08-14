using Hakeem.Application.Repositories.PushDevices;
using Hakeem.Domain.Entities;
using Hakeem.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Hakeem.Infrastructure.Repositories.PushDevices;

public sealed class PushDeviceRepository(ApplicationDbContext dbContext)
    : IPushDeviceRepository
{
    public Task<PushDevice?> GetByExpoPushTokenAsync(
        string expoPushToken,
        CancellationToken cancellationToken)
    {
        return dbContext.PushDevices.SingleOrDefaultAsync(
            device => device.ExpoPushToken == expoPushToken,
            cancellationToken);
    }

    public async Task<IReadOnlyList<PushDevice>> GetActiveForUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await dbContext.PushDevices
            .Where(device => device.UserId == userId && device.IsActive)
            .OrderBy(device => device.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public void Add(PushDevice pushDevice)
    {
        dbContext.PushDevices.Add(pushDevice);
    }
}
