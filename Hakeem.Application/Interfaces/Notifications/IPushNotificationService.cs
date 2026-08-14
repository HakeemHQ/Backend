namespace Hakeem.Application.Interfaces.Notifications;

public interface IPushNotificationService
{
    Task<PushNotificationSendResult> SendAsync(
        PushNotificationMessage message,
        CancellationToken cancellationToken);
}

public sealed record PushNotificationMessage(
    IReadOnlyCollection<string> DeviceTokens,
    string Title,
    string Body,
    IReadOnlyDictionary<string, string> Data);

public sealed record PushNotificationSendResult(
    IReadOnlyCollection<string> InactiveDeviceTokens)
{
    public static PushNotificationSendResult Success { get; } = new([]);
}
