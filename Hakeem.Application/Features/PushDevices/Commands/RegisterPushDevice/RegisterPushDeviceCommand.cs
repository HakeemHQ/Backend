using MediatR;

namespace Hakeem.Application.Features.PushDevices.Commands.RegisterPushDevice;

public sealed class RegisterPushDeviceCommand
    : IRequest<RegisterPushDeviceResult>
{
    public string ExpoPushToken { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
}

public sealed record RegisterPushDeviceResult(
    Guid Id,
    string ExpoPushToken,
    string Platform,
    string Language,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
