using MediatR;

namespace Hakeem.Application.Features.PushDevices.Commands.UnregisterPushDevice;

public sealed class UnregisterPushDeviceCommand : IRequest<Unit>
{
    public string ExpoPushToken { get; set; } = string.Empty;
}
