using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Repositories.PushDevices;
using Hakeem.Domain.Interfaces;
using MediatR;

namespace Hakeem.Application.Features.PushDevices.Commands.UnregisterPushDevice;

public sealed class UnregisterPushDeviceCommandHandler(
    IPushDeviceRepository pushDeviceRepository,
    ICurrentUserContext currentUserContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UnregisterPushDeviceCommand, Unit>
{
    public async Task<Unit> Handle(
        UnregisterPushDeviceCommand request,
        CancellationToken cancellationToken)
    {
        var pushDevice = await pushDeviceRepository.GetByExpoPushTokenAsync(
            request.ExpoPushToken.Trim(),
            cancellationToken);

        if (pushDevice is not null &&
            pushDevice.UserId == currentUserContext.UserId &&
            pushDevice.IsActive)
        {
            pushDevice.IsActive = false;
            await unitOfWork.SaveChanges(cancellationToken);
        }

        return Unit.Value;
    }
}
