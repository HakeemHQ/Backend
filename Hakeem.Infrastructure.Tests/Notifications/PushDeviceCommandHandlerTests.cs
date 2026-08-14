using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Features.PushDevices.Commands.RegisterPushDevice;
using Hakeem.Application.Features.PushDevices.Commands.UnregisterPushDevice;
using Hakeem.Application.Repositories.PushDevices;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces;

namespace Hakeem.Infrastructure.Tests.Notifications;

public sealed class PushDeviceCommandHandlerTests
{
    [Fact]
    public async Task Register_ReactivatesAndMovesExistingTokenToCurrentUser()
    {
        var currentUserId = Guid.NewGuid();
        var existing = new PushDevice
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ExpoPushToken = "ExponentPushToken[existing]",
            Platform = "ios",
            Language = "en",
            IsActive = false
        };
        var repository = new FakePushDeviceRepository([existing]);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RegisterPushDeviceCommandHandler(
            repository,
            new FakeCurrentUserContext(currentUserId),
            unitOfWork);

        var result = await handler.Handle(
            new RegisterPushDeviceCommand
            {
                ExpoPushToken = " ExponentPushToken[existing] ",
                Platform = "ANDROID",
                Language = "AR"
            },
            CancellationToken.None);

        Assert.Equal(currentUserId, existing.UserId);
        Assert.Equal("android", existing.Platform);
        Assert.Equal("ar", existing.Language);
        Assert.Equal("ar", result.Language);
        Assert.True(existing.IsActive);
        Assert.Equal(existing.Id, result.Id);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Register_AddsDifferentTokensAsMultipleDevicesForUser()
    {
        var userId = Guid.NewGuid();
        var repository = new FakePushDeviceRepository([]);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RegisterPushDeviceCommandHandler(
            repository,
            new FakeCurrentUserContext(userId),
            unitOfWork);

        await handler.Handle(
            new RegisterPushDeviceCommand
            {
                ExpoPushToken = "ExponentPushToken[first]",
                Platform = "ios",
                Language = "en"
            },
            CancellationToken.None);
        await handler.Handle(
            new RegisterPushDeviceCommand
            {
                ExpoPushToken = "ExponentPushToken[second]",
                Platform = "android",
                Language = "ar"
            },
            CancellationToken.None);

        Assert.Equal(2, repository.Devices.Count);
        Assert.All(repository.Devices, device => Assert.Equal(userId, device.UserId));
        Assert.Equal(2, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Unregister_DeactivatesOnlyTokenOwnedByCurrentUser()
    {
        var userId = Guid.NewGuid();
        var owned = CreateDevice(userId, "ExponentPushToken[owned]");
        var other = CreateDevice(Guid.NewGuid(), "ExponentPushToken[other]");
        var repository = new FakePushDeviceRepository([owned, other]);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UnregisterPushDeviceCommandHandler(
            repository,
            new FakeCurrentUserContext(userId),
            unitOfWork);

        await handler.Handle(
            new UnregisterPushDeviceCommand
            {
                ExpoPushToken = owned.ExpoPushToken
            },
            CancellationToken.None);
        await handler.Handle(
            new UnregisterPushDeviceCommand
            {
                ExpoPushToken = other.ExpoPushToken
            },
            CancellationToken.None);

        Assert.False(owned.IsActive);
        Assert.True(other.IsActive);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    private static PushDevice CreateDevice(Guid userId, string token) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        ExpoPushToken = token,
        Platform = "android",
        Language = "en",
        IsActive = true
    };

    private sealed record FakeCurrentUserContext(Guid UserId) : ICurrentUserContext;

    private sealed class FakePushDeviceRepository(
        IEnumerable<PushDevice> devices) : IPushDeviceRepository
    {
        public List<PushDevice> Devices { get; } = devices.ToList();

        public Task<PushDevice?> GetByExpoPushTokenAsync(
            string expoPushToken,
            CancellationToken cancellationToken) =>
            Task.FromResult(Devices.SingleOrDefault(
                device => device.ExpoPushToken == expoPushToken));

        public Task<IReadOnlyList<PushDevice>> GetActiveForUserAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PushDevice>>(
                Devices.Where(device =>
                    device.UserId == userId && device.IsActive).ToList());

        public void Add(PushDevice pushDevice)
        {
            Devices.Add(pushDevice);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChanges() => SaveChanges(CancellationToken.None);

        public Task<int> SaveChanges(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.FromResult(1);
        }

        public Task BeginTransactionAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task CommitTransactionAsync() => Task.CompletedTask;

        public Task RollBackTransactionAsync() => Task.CompletedTask;

        public void Dispose()
        {
        }
    }
}
