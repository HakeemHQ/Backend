using MediatR;

namespace Hakeem.Application.Features.DoctorPatientAccesses.Commands.RevokeDoctorPatientAccess;

public sealed record RevokeDoctorPatientAccessCommand(Guid AccessId)
    : IRequest;
