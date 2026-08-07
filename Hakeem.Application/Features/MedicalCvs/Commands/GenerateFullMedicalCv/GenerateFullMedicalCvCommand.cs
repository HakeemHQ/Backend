using MediatR;

namespace Hakeem.Application.Features.MedicalCvs.Commands.GenerateFullMedicalCv;

public sealed record GenerateFullMedicalCvCommand(string Title)
    : IRequest<GenerateFullMedicalCvResponse>;
