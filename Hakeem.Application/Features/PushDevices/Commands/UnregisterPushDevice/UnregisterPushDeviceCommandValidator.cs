using FluentValidation;
using Hakeem.Application.Resources;
using Microsoft.Extensions.Localization;

namespace Hakeem.Application.Features.PushDevices.Commands.UnregisterPushDevice;

public sealed class UnregisterPushDeviceCommandValidator
    : AbstractValidator<UnregisterPushDeviceCommand>
{
    public UnregisterPushDeviceCommandValidator(
        IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(command => command.ExpoPushToken)
            .NotEmpty()
            .MaximumLength(512)
            .Matches(@"^(Expo|Exponent)PushToken\[[^\]\s]+\]$")
            .WithMessage(localizer["PushDevice.InvalidExpoPushToken"].Value);
    }
}
