using MediatR;

namespace Hakeem.Application.Features.MedicalIntelligence.Commands.Chat;

public sealed record MedicalIntelligenceChatCommand(string Message)
    : IRequest<MedicalIntelligenceChatResponse>;
