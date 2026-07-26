using FluentValidation;
using Hakeem.Application.Constants;
using Hakeem.Application.Resources;
using Microsoft.Extensions.Localization;

namespace Hakeem.Application.Features.Auth.Commands.Refresh;

public sealed class RefreshCommandValidator : AbstractValidator<RefreshCommand>
{
    private const int RefreshTokenLength = 86;

    public RefreshCommandValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(command => command.RefreshToken)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage(localizer[ErrorCodes.ValidationRequired].Value)
            .WithErrorCode(ErrorCodes.ValidationRequired)
            .Length(RefreshTokenLength)
            .WithMessage(localizer["Validation.InvalidRefreshTokenFormat"].Value)
            .WithErrorCode("Validation.InvalidRefreshTokenFormat")
            .Matches("^[A-Za-z0-9_-]+$")
            .WithMessage(localizer["Validation.InvalidRefreshTokenFormat"].Value)
            .WithErrorCode("Validation.InvalidRefreshTokenFormat");
    }
}
