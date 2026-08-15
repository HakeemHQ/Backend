using MediatR;
using Hakeem.Domain.Enums.Identity;

namespace Hakeem.Application.Features.Admin.Doctors.Commands.UpdateDoctorStatus
{
    public sealed record UpdateDoctorStatusCommand(
        Guid DoctorId,
        string Status)
        : IRequest<UpdateDoctorStatusResult>;

    public sealed record UpdateDoctorStatusResult(
        Guid DoctorId,
        string Status);
}
