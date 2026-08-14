using FluentValidation;
using Hakeem.Application.Resources;
using Microsoft.Extensions.Localization;

namespace Hakeem.Application.Features.PushDevices.Commands.RegisterPushDevice;

public sealed class RegisterPushDeviceCommandValidator
    : AbstractValidator<RegisterPushDeviceCommand>
{
    public RegisterPushDeviceCommandValidator(
        IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(command => command.ExpoPushToken)
            .NotEmpty()
            .MaximumLength(512)
            .Matches(@"^(Expo|Exponent)PushToken\[[^\]\s]+\]$")
            .WithMessage(localizer["PushDevice.InvalidExpoPushToken"].Value);

        RuleFor(command => command.Platform)
            .NotEmpty()
            .Must(platform =>
                string.Equals(platform?.Trim(), "ios", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(platform?.Trim(), "android", StringComparison.OrdinalIgnoreCase))
            .WithMessage(localizer["PushDevice.InvalidPlatform"].Value);

        RuleFor(command => command.Language)
            .NotEmpty()
            .Must(language =>
                string.Equals(language?.Trim(), "en", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(language?.Trim(), "ar", StringComparison.OrdinalIgnoreCase))
            .WithMessage(localizer["PushDevice.InvalidLanguage"].Value);
    }
}
