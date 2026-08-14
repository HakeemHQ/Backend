using Hakeem.Application.Abstractions;
using Hakeem.Application.Interfaces.Notifications;
using Hakeem.Application.Repositories.PushDevices;
using Hakeem.Application.Resources;
using Hakeem.Domain.DomainEvents.Outbox;

namespace Hakeem.Application.Projections.PushNotificationHandlers;

public sealed class PatientAccessRequestedEventHandler(
    IPushDeviceRepository pushDeviceRepository,
    IPushNotificationService pushNotificationService)
    : IOutboxEventHandler<PatientAccessRequestedEvent>
{
    public async Task HandleAsync(
        PatientAccessRequestedEvent @event,
        CancellationToken cancellationToken)
    {
        var devices = await pushDeviceRepository.GetActiveForUserAsync(
            @event.PatientUserId,
            cancellationToken);

        if (devices.Count == 0)
        {
            return;
        }

        var inactiveTokens = new HashSet<string>(StringComparer.Ordinal);

        foreach (var languageGroup in devices.GroupBy(device => device.Language))
        {
            var language = languageGroup.Key;
            var message = new PushNotificationMessage(
                languageGroup
                    .Select(device => device.ExpoPushToken)
                    .ToArray(),
                LocalizedResourceText.Get(
                    "PushNotification.PatientAccessRequested.Title",
                    language),
                LocalizedResourceText.Get(
                    "PushNotification.PatientAccessRequested.Body",
                    language),
                new Dictionary<string, string>
                {
                    ["type"] = "PatientAccessRequest",
                    ["requestId"] = @event.RequestId.ToString()
                });

            var result = await pushNotificationService.SendAsync(
                message,
                cancellationToken);

            inactiveTokens.UnionWith(result.InactiveDeviceTokens);
        }

        foreach (var device in devices.Where(device =>
                     inactiveTokens.Contains(device.ExpoPushToken)))
        {
            device.IsActive = false;
        }
    }
}
