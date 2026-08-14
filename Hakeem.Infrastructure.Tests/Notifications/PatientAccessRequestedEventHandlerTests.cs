using Hakeem.Application.Interfaces.Notifications;
using Hakeem.Application.Projections.PushNotificationHandlers;
using Hakeem.Application.Repositories.PushDevices;
using Hakeem.Domain.DomainEvents.Outbox;
using Hakeem.Domain.Entities;

namespace Hakeem.Infrastructure.Tests.Notifications;

public sealed class PatientAccessRequestedEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_SendsSafePayloadToEveryActiveDevice()
    {
        var userId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var first = CreateDevice(userId, "ExponentPushToken[first]");
        var second = CreateDevice(userId, "ExpoPushToken[second]");
        var repository = new FakePushDeviceRepository([first, second]);
        var service = new FakePushNotificationService(
            new PushNotificationSendResult([second.ExpoPushToken]));
        var handler = new PatientAccessRequestedEventHandler(repository, service);

        await handler.HandleAsync(
            new PatientAccessRequestedEvent
            {
                RequestId = requestId,
                PatientUserId = userId
            },
            CancellationToken.None);

        var message = Assert.IsType<PushNotificationMessage>(service.Message);
        Assert.Equal(2, message.DeviceTokens.Count);
        Assert.Contains(first.ExpoPushToken, message.DeviceTokens);
        Assert.Contains(second.ExpoPushToken, message.DeviceTokens);
        Assert.Equal("Doctor access request", message.Title);
        Assert.Equal(
            "A doctor is requesting temporary access to your Hakeem records. " +
            "Open Hakeem to review the request.",
            message.Body);
        Assert.Equal("PatientAccessRequest", message.Data["type"]);
        Assert.Equal(requestId.ToString(), message.Data["requestId"]);
        Assert.Equal(2, message.Data.Count);
        Assert.True(first.IsActive);
        Assert.False(second.IsActive);
    }

    [Fact]
    public async Task HandleAsync_GroupsDevicesAndLocalizesByStoredLanguage()
    {
        var userId = Guid.NewGuid();
        var repository = new FakePushDeviceRepository(
        [
            CreateDevice(userId, "ExponentPushToken[english]", "en"),
            CreateDevice(userId, "ExponentPushToken[arabic]", "ar")
        ]);
        var service = new FakePushNotificationService(
            PushNotificationSendResult.Success);
        var handler = new PatientAccessRequestedEventHandler(repository, service);

        await handler.HandleAsync(
            new PatientAccessRequestedEvent
            {
                RequestId = Guid.NewGuid(),
                PatientUserId = userId
            },
            CancellationToken.None);

        Assert.Equal(2, service.Messages.Count);
        var english = Assert.Single(service.Messages, message =>
            message.DeviceTokens.Contains("ExponentPushToken[english]"));
        Assert.Equal("Doctor access request", english.Title);
        Assert.Equal(
            "A doctor is requesting temporary access to your Hakeem records. " +
            "Open Hakeem to review the request.",
            english.Body);

        var arabic = Assert.Single(service.Messages, message =>
            message.DeviceTokens.Contains("ExponentPushToken[arabic]"));
        Assert.Equal("طلب وصول من طبيب", arabic.Title);
        Assert.Equal(
            "يرغب طبيب في الوصول مؤقتًا إلى سجلاتك في حكيم. " +
            "افتح حكيم لمراجعة الطلب.",
            arabic.Body);
    }

    [Fact]
    public async Task HandleAsync_WithNoActiveDevices_DoesNotCallService()
    {
        var service = new FakePushNotificationService(
            PushNotificationSendResult.Success);
        var handler = new PatientAccessRequestedEventHandler(
            new FakePushDeviceRepository([]),
            service);

        await handler.HandleAsync(
            new PatientAccessRequestedEvent
            {
                RequestId = Guid.NewGuid(),
                PatientUserId = Guid.NewGuid()
            },
            CancellationToken.None);

        Assert.Null(service.Message);
    }

    [Fact]
    public async Task HandleAsync_WhenPushServiceFails_PropagatesForOutboxRetry()
    {
        var userId = Guid.NewGuid();
        var expected = new HttpRequestException("transient");
        var service = new FakePushNotificationService(expected);
        var handler = new PatientAccessRequestedEventHandler(
            new FakePushDeviceRepository(
                [CreateDevice(userId, "ExponentPushToken[first]")]),
            service);

        var actual = await Assert.ThrowsAsync<HttpRequestException>(() =>
            handler.HandleAsync(
                new PatientAccessRequestedEvent
                {
                    RequestId = Guid.NewGuid(),
                    PatientUserId = userId
                },
                CancellationToken.None));

        Assert.Same(expected, actual);
    }

    private static PushDevice CreateDevice(
        Guid userId,
        string token,
        string language = "en") => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        ExpoPushToken = token,
        Platform = "android",
        Language = language,
        IsActive = true
    };

    private sealed class FakePushDeviceRepository(
        IReadOnlyList<PushDevice> devices) : IPushDeviceRepository
    {
        public Task<PushDevice?> GetByExpoPushTokenAsync(
            string expoPushToken,
            CancellationToken cancellationToken) =>
            Task.FromResult(devices.SingleOrDefault(
                device => device.ExpoPushToken == expoPushToken));

        public Task<IReadOnlyList<PushDevice>> GetActiveForUserAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PushDevice>>(
                devices.Where(device =>
                    device.UserId == userId && device.IsActive).ToList());

        public void Add(PushDevice pushDevice) =>
            throw new NotSupportedException();
    }

    private sealed class FakePushNotificationService
        : IPushNotificationService
    {
        private readonly PushNotificationSendResult? _result;
        private readonly Exception? _exception;

        public FakePushNotificationService(PushNotificationSendResult result)
        {
            _result = result;
        }

        public FakePushNotificationService(Exception exception)
        {
            _exception = exception;
        }

        public List<PushNotificationMessage> Messages { get; } = [];
        public PushNotificationMessage? Message => Messages.SingleOrDefault();

        public Task<PushNotificationSendResult> SendAsync(
            PushNotificationMessage message,
            CancellationToken cancellationToken)
        {
            Messages.Add(message);
            return _exception is not null
                ? Task.FromException<PushNotificationSendResult>(_exception)
                : Task.FromResult(_result!);
        }
    }
}
