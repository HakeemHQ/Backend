using FluentValidation;

namespace Hakeem.Application.Features.MedicalIntelligence.Commands.Chat;

public sealed class MedicalIntelligenceChatCommandValidator
    : AbstractValidator<MedicalIntelligenceChatCommand>
{
    public MedicalIntelligenceChatCommandValidator()
    {
        RuleFor(command => command.Message)
            .NotEmpty()
            .MaximumLength(4_000);
    }
}
