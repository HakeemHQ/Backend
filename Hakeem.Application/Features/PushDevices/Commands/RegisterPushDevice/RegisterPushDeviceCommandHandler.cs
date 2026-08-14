using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Repositories.PushDevices;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces;
using MediatR;

namespace Hakeem.Application.Features.PushDevices.Commands.RegisterPushDevice;

public sealed class RegisterPushDeviceCommandHandler(
    IPushDeviceRepository pushDeviceRepository,
    ICurrentUserContext currentUserContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterPushDeviceCommand, RegisterPushDeviceResult>
{
    public async Task<RegisterPushDeviceResult> Handle(
        RegisterPushDeviceCommand request,
        CancellationToken cancellationToken)
    {
        var token = request.ExpoPushToken.Trim();
        var platform = request.Platform.Trim().ToLowerInvariant();
        var language = request.Language.Trim().ToLowerInvariant();
        var userId = currentUserContext.UserId;

        var pushDevice = await pushDeviceRepository.GetByExpoPushTokenAsync(
            token,
            cancellationToken);

        if (pushDevice is null)
        {
            pushDevice = new PushDevice
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ExpoPushToken = token,
                Platform = platform,
                Language = language,
                IsActive = true
            };

            pushDeviceRepository.Add(pushDevice);
        }
        else
        {
            // A device may change accounts. Move the globally unique token to
            // the currently authenticated user so the prior account cannot
            // continue sending notifications to that device.
            pushDevice.UserId = userId;
            pushDevice.Platform = platform;
            pushDevice.Language = language;
            pushDevice.IsActive = true;
        }

        await unitOfWork.SaveChanges(cancellationToken);

        return new RegisterPushDeviceResult(
            pushDevice.Id,
            pushDevice.ExpoPushToken,
            pushDevice.Platform,
            pushDevice.Language,
            pushDevice.IsActive,
            pushDevice.CreatedAt,
            pushDevice.UpdatedAt);
    }
}
